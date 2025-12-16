using UnityEngine;

namespace RuntimeModelLoaders.Interaction
{
    /// <summary>
    /// Hält Metadaten zur geladenen Datei, um sie später zu identifizieren
    /// (z. B. für UI-Listen oder Debugging).
    /// </summary>
    public class ModelMetadata : MonoBehaviour
    {
        public string SourcePath { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}
