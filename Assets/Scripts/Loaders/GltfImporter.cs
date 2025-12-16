using System;
using System.Collections;
using GLTFast;
using UnityEngine;

namespace RuntimeModelLoaders.Loaders
{
    /// <summary>
    /// Lädt glTF/GLB-Dateien über glTFast und instanziiert die Szene unterhalb eines Parents.
    /// Der Import ist coroutine-basiert, damit die UI responsiv bleibt.
    /// </summary>
    public static class GltfImporter
    {
        public static IEnumerator Load(string path, Transform parent, Action<GameObject> onSuccess)
        {
            var gltf = new GltfImport();
            var loadTask = gltf.Load(path, new ImportSettings
            {
                generateMipMaps = true,
                anisotropicFilterLevel = 4
            });

            while (!loadTask.IsCompleted)
                yield return null;

            if (!loadTask.Result)
                throw new InvalidOperationException($"Konnte glTF nicht laden: {path}");

            var root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));

            var instantiated = gltf.InstantiateMainScene(root.transform);
            if (!instantiated)
                throw new InvalidOperationException($"glTF Szene konnte nicht instanziiert werden: {path}");

            root.transform.SetParent(parent, worldPositionStays: true);
            onSuccess?.Invoke(root);
        }
    }
}
