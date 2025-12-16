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
    /// Koordiniert das Laden von OBJ/STL-Dateien aus persistentem Speicher oder StreamingAssets
    /// und instanziiert sie im VR-Raum. Die Klasse trennt UI-Events von Loader-Logik.
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

        public bool IncludeStreamingAssets => includeStreamingAssets;

        private readonly List<GameObject> _spawnedModels = new();
        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".obj", ".stl"
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
        public void LoadFile(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                toastPresenter?.ShowMessage("Keine Datei ausgewählt.");
                return;
            }

            if (!File.Exists(fullPath))
            {
                toastPresenter?.ShowMessage($"Datei nicht gefunden: {fullPath}");
                return;
            }

            var extension = Path.GetExtension(fullPath);
            if (!SupportedExtensions.Contains(extension))
            {
                toastPresenter?.ShowMessage($"Nicht unterstütztes Format: {extension}");
                return;
            }

            try
            {
                var mesh = CreateMesh(fullPath, extension);
                SpawnModel(mesh, fullPath);
                toastPresenter?.ShowMessage($"Geladen: {Path.GetFileName(fullPath)}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                toastPresenter?.ShowMessage($"Fehler beim Laden: {ex.Message}");
            }
        }

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

        private Mesh CreateMesh(string path, string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".obj" => ObjImporter.Parse(path),
                ".stl" => StlImporter.Parse(path),
                _ => throw new NotSupportedException($"Extension {extension} wird nicht unterstützt.")
            };
        }

        private void SpawnModel(Mesh mesh, string path)
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

            var interactable = go.AddComponent<ModelPlacementController>();
            interactable.movementType = XRBaseInteractable.MovementType.VelocityTracking;

            var metadata = go.AddComponent<ModelMetadata>();
            metadata.SourcePath = path;
            metadata.DisplayName = Path.GetFileName(path);

            // Positioniere das Modell vor dem XR Origin (falls vorhanden)
            var xrOrigin = Camera.main?.transform;
            if (xrOrigin != null)
            {
                go.transform.position = xrOrigin.position + xrOrigin.forward * 1.0f;
                go.transform.rotation = Quaternion.LookRotation(xrOrigin.forward, Vector3.up);
            }

            go.transform.SetParent(contentRoot, worldPositionStays: true);
            _spawnedModels.Add(go);
        }

        private IEnumerable<string> EnumerateFiles(string root, bool isStreamingAssets = false)
        {
            if (!Directory.Exists(root))
                yield break;

            foreach (var file in Directory.GetFiles(root))
            {
                var ext = Path.GetExtension(file);
                if (SupportedExtensions.Contains(ext))
                {
                    // Kennzeichne StreamingAssets-Pfade für UI
                    yield return isStreamingAssets ? $"[StreamingAssets] {file}" : file;
                }
            }
        }
    }
}
