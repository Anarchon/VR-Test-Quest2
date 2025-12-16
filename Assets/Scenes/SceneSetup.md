# SampleLoaderScene Setup

1. Lege eine neue Szene an und speichere sie als `SampleLoaderScene` im Ordner `Assets/Scenes/`.
2. Füge ein `XR Origin (Action Based)` Prefab aus dem XR Interaction Toolkit hinzu.
3. Platziere einen World-Space-Canvas (ca. 0.8m vor dem Startpunkt) und weise das Skript `FileBrowserUI` zu.
   - Dropdown/Text/Button-Referenzen per Drag & Drop setzen.
   - `ToastPresenter` auf demselben Canvas platzieren (CanvasGroup + Text).
4. Erstelle ein leeres GameObject `ModelLoader`, hänge `RuntimeModelLoader` an und befülle
   `Content Root` (leeres Transform), `Default Material` und die UI-Referenzen.
5. Optional: Teleportation Areas hinzufügen, um sich um die geladenen Modelle zu bewegen.
