using System;
using System.Collections.Generic;
using System.IO;
using RuntimeModelLoaders.Interaction;
using RuntimeModelLoaders.UI;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace RuntimeModelLoaders.Loaders
{
    /// <summary>
    /// Koordiniert das Laden von OBJ/STL/glTF-Dateien aus dem Models-Ordner im Spieleverzeichnis
    /// (persistentDataPath) oder optional StreamingAssets/Models und instanziiert sie im VR-Raum.
    /// Die Klasse trennt UI-Events von Loader-Logik.
    /// </summary>
    public class RuntimeModelLoader : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private FileBrowserUI fileBrowserUI;
        [SerializeField] private ToastPresenter toastPresenter;

        [Header("Options")]
        [Tooltip("Falls gesetzt, werden Dateien aus StreamingAssets zusätzlich angezeigt.")]
        [SerializeField] private bool includeStreamingAssets = true;
        [Tooltip("Name des Unterordners innerhalb des Spielordners, der Modelldateien enthält.")]
        [SerializeField] private string modelsFolderName = "Models";

        public bool IncludeStreamingAssets => includeStreamingAssets;

        private readonly List<GameObject> _spawnedModels = new();
        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".obj", ".stl", ".gltf", ".glb"
        };

        private void Start()
        {
            if (fileBrowserUI != null)
                fileBrowserUI.BindLoader(this);

            RefreshFileList();
        }

        public void SetIncludeStreamingAssets(bool enabled)
        {
            includeStreamingAssets = enabled;
            RefreshFileList();
        }

        /// <summary>
        /// Sammelt alle unterstützten Dateien aus persistentDataPath und optional StreamingAssets
        /// und aktualisiert die UI-Auswahl.
        /// </summary>
        public void RefreshFileList()
        {
            var entries = new List<string>();
            entries.AddRange(EnumerateFiles(Application.persistentDataPath));

            if (includeStreamingAssets && Directory.Exists(Application.streamingAssetsPath))
            {
                entries.AddRange(EnumerateFiles(Application.streamingAssetsPath, true));
            }

            entries.Sort(StringComparer.OrdinalIgnoreCase);
            fileBrowserUI?.SetFiles(entries);
        }

        /// <summary>
        /// Lädt die ausgewählte Datei, erzeugt ein GameObject mit MeshRenderer/MeshCollider/XRGrabInteractable
        /// und plaziert es vor dem XR Origin.
        /// </summary>
        /// <param name="fullPath">Absoluter Pfad zur Datei</param>
        public void LoadFile(string fullPath) => StartCoroutine(LoadFileRoutine(fullPath));

        /// <summary>
        /// Entfernt alle zuvor instanziierten Modelle.
        /// </summary>
        public void ClearAll()
        {
            foreach (var go in _spawnedModels)
            {
                if (go != null)
                    Destroy(go);
            }
            _spawnedModels.Clear();
        }

        private System.Collections.IEnumerator LoadFileRoutine(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                toastPresenter?.ShowMessage("Keine Datei ausgewählt.");
                yield break;
            }

            var cleanedPath = NormalizePath(fullPath);

            if (!File.Exists(cleanedPath))
            {
                toastPresenter?.ShowMessage($"Datei nicht gefunden: {cleanedPath}");
                yield break;
            }

            var extension = Path.GetExtension(cleanedPath);
            if (!SupportedExtensions.Contains(extension))
            {
                toastPresenter?.ShowMessage($"Nicht unterstütztes Format: {extension}");
                yield break;
            }

            Exception? caught = null;
            bool success = false;

            yield return LoadFileInternal(cleanedPath, extension, ex => caught = ex, () => success = true);

            if (caught != null)
            {
                Debug.LogException(caught);
                toastPresenter?.ShowMessage($"Fehler beim Laden: {caught.Message}");
            }
            else if (success)
            {
                toastPresenter?.ShowMessage($"Geladen: {Path.GetFileName(cleanedPath)}");
            }
        }

        private System.Collections.IEnumerator LoadFileInternal(
            string cleanedPath,
            string extension,
            Action<Exception> onError,
            Action onSuccess)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".gltf":
                case ".glb":
                    var gltfRoutine = GltfImporter.Load(cleanedPath, contentRoot, go =>
                    {
                        FinalizeSpawn(go, cleanedPath);
                        onSuccess?.Invoke();
                    });

                    while (true)
                    {
                        bool moveNext;
                        try
                        {
                            moveNext = gltfRoutine.MoveNext();
                        }
                        catch (Exception ex)
                        {
                            onError?.Invoke(ex);
                            yield break;
                        }

                        if (!moveNext)
                            break;

                        yield return gltfRoutine.Current;
                    }

                    break;
                default:
                    try
                    {
                        var mesh = CreateMesh(cleanedPath, extension);
                        var go = CreateMeshObject(mesh);
                        FinalizeSpawn(go, cleanedPath);
                        onSuccess?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke(ex);
                    }

                    break;
            }
        }

        private Mesh CreateMesh(string path, string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".obj" => ObjImporter.Parse(path),
                ".stl" => StlImporter.Parse(path),
                _ => throw new NotSupportedException($"Extension {extension} wird nicht unterstützt.")
            };
        }

        private GameObject CreateMeshObject(Mesh mesh)
        {
            var go = new GameObject(mesh.name);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = defaultMaterial != null
                ? defaultMaterial
                : new Material(Shader.Find("Universal Render Pipeline/Lit"));

            var collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = true;

            return go;
        }

        private void FinalizeSpawn(GameObject root, string path)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            EnsureColliders(root);
            AttachInteraction(root);
            AttachMetadata(root, path);
            PositionInFrontOfCamera(root.transform);

            root.transform.SetParent(contentRoot, worldPositionStays: true);
            _spawnedModels.Add(root);
        }

        private void AttachInteraction(GameObject go)
        {
            var interactable = go.GetComponent<ModelPlacementController>();
            if (interactable == null)
            {
                interactable = go.AddComponent<ModelPlacementController>();
                interactable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            }
        }

        private void AttachMetadata(GameObject go, string path)
        {
            var metadata = go.GetComponent<ModelMetadata>();
            if (metadata == null)
                metadata = go.AddComponent<ModelMetadata>();

            metadata.SourcePath = path;
            metadata.DisplayName = Path.GetFileName(path);
        }

        private void EnsureColliders(GameObject root)
        {
            var filters = root.GetComponentsInChildren<MeshFilter>();
            foreach (var filter in filters)
            {
                if (filter.sharedMesh == null)
                    continue;

                var collider = filter.GetComponent<MeshCollider>();
                if (collider == null)
                {
                    collider = filter.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = filter.sharedMesh;
                }

                collider.convex = true;
            }
        }

        private void PositionInFrontOfCamera(Transform target)
        {
            var xrOrigin = Camera.main?.transform;
            if (xrOrigin != null)
            {
                target.position = xrOrigin.position + xrOrigin.forward * 1.0f;
                target.rotation = Quaternion.LookRotation(xrOrigin.forward, Vector3.up);
            }
        }

        private IEnumerable<string> EnumerateFiles(string root, bool isStreamingAssets = false)
        {
            if (!Directory.Exists(root))
                yield break;

            var modelsRoot = Path.Combine(root, modelsFolderName);
            if (root == Application.persistentDataPath && !Directory.Exists(modelsRoot))
            {
                Directory.CreateDirectory(modelsRoot);
            }

            if (!Directory.Exists(modelsRoot))
                yield break;

            foreach (var dir in Directory.GetDirectories(modelsRoot))
            {
                var file = FindFirstSupportedFile(dir);
                if (file != null)
                    yield return isStreamingAssets ? $"[StreamingAssets] {file}" : file;
            }

            foreach (var file in Directory.GetFiles(modelsRoot))
            {
                var ext = Path.GetExtension(file);
                if (SupportedExtensions.Contains(ext))
                    yield return isStreamingAssets ? $"[StreamingAssets] {file}" : file;
            }
        }

        private string NormalizePath(string fullPath)
        {
            return fullPath.StartsWith("[StreamingAssets]", StringComparison.OrdinalIgnoreCase)
                ? fullPath.Replace("[StreamingAssets]", string.Empty).Trim()
                : fullPath;
        }

        private string? FindFirstSupportedFile(string directory)
        {
            foreach (var file in Directory.GetFiles(directory))
            {
                var ext = Path.GetExtension(file);
                if (SupportedExtensions.Contains(ext))
                    return file;
            }

            return null;
        }
    }
}
