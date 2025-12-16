using System;
using System.Collections.Generic;
using System.IO;
using RuntimeModelLoaders.Loaders;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeModelLoaders.UI
{
    /// <summary>
    /// Steuert eine World-Space-UI bestehend aus Dropdown + Buttons. Das Script ist
    /// bewusst simpel gehalten, damit es mit dem Unity UI (ohne TMP) auskommt.
    /// </summary>
    public class FileBrowserUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Dropdown fileDropdown;
        [SerializeField] private Toggle streamingAssetsToggle;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private ToastPresenter toastPresenter;

        private RuntimeModelLoader? _loader;
        private readonly List<string> _files = new();

        private void Awake()
        {
            // Basic Safety: Buttons verdrahten, falls noch nicht im Editor gesetzt.
            if (refreshButton != null) refreshButton.onClick.AddListener(OnRefreshClicked);
            if (loadButton != null) loadButton.onClick.AddListener(OnLoadClicked);
            if (clearButton != null) clearButton.onClick.AddListener(OnClearClicked);
            if (streamingAssetsToggle != null) streamingAssetsToggle.onValueChanged.AddListener(OnStreamingAssetsToggled);
        }

        public void BindLoader(RuntimeModelLoader loader)
        {
            _loader = loader;
            if (streamingAssetsToggle != null)
                streamingAssetsToggle.isOn = loader.IncludeStreamingAssets;
        }

        public void SetFiles(List<string> files)
        {
            _files.Clear();
            _files.AddRange(files);

            if (fileDropdown == null)
                return;

            fileDropdown.ClearOptions();
            var options = new List<Dropdown.OptionData>();
            foreach (var file in _files)
            {
                var label = FormatLabel(file);
                options.Add(new Dropdown.OptionData(label));
            }
            if (options.Count == 0)
                options.Add(new Dropdown.OptionData("<keine Dateien gefunden>"));

            fileDropdown.AddOptions(options);
            fileDropdown.value = 0;
        }

        private void OnRefreshClicked()
        {
            _loader?.RefreshFileList();
        }

        private void OnLoadClicked()
        {
            if (_loader == null)
            {
                toastPresenter?.ShowMessage("Loader nicht gesetzt.");
                return;
            }

            if (_files.Count == 0)
            {
                toastPresenter?.ShowMessage("Keine Dateien gefunden.");
                return;
            }

            var index = Mathf.Clamp(fileDropdown?.value ?? 0, 0, _files.Count - 1);
            var path = _files[index];

            // Optional: StreamingAssets-Pfad wieder zurückrechnen
            if (path.StartsWith("[StreamingAssets]", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Replace("[StreamingAssets]", string.Empty).Trim();
            }

            _loader.LoadFile(path);
        }

        private void OnClearClicked()
        {
            _loader?.ClearAll();
        }

        private void OnStreamingAssetsToggled(bool enabled)
        {
            _loader?.SetIncludeStreamingAssets(enabled);
        }

        private static string FormatLabel(string path)
        {
            var cleaned = path.StartsWith("[StreamingAssets]", StringComparison.OrdinalIgnoreCase)
                ? path.Replace("[StreamingAssets]", string.Empty).Trim()
                : path;

            var fileName = Path.GetFileName(cleaned);
            var folderName = Path.GetFileName(Path.GetDirectoryName(cleaned));

            var label = string.IsNullOrEmpty(folderName) ? fileName : $"{folderName}/{fileName}";

            if (path.StartsWith("[StreamingAssets]", StringComparison.OrdinalIgnoreCase) ||
                path.Contains(Application.streamingAssetsPath, StringComparison.OrdinalIgnoreCase))
            {
                label = $"StreamingAssets: {label}";
            }

            return label;
        }
    }
}
