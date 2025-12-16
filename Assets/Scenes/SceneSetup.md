# SampleLoaderScene Setup

1. Öffne `Assets/Scenes/SampleLoaderScene.unity`. Sie enthält bereits ein `ContentRoot`, das vor der Kamera platziert ist, sowie
   den `ModelLoader` mit referenziertem `DefaultModel`-Material.
2. Ersetze die `Main Camera` durch ein `XR Origin (Action Based)` Prefab aus dem XR Interaction Toolkit und aktiviere Ray- plus
   Direct-Interactor.
3. Platziere einen World-Space-Canvas (ca. 0.8m vor dem Startpunkt) und weise das Skript `FileBrowserUI` zu.
   - Dropdown/Text/Button-Referenzen per Drag & Drop setzen.
   - `ToastPresenter` auf demselben Canvas platzieren (CanvasGroup + Text).
4. Verbinde den `RuntimeModelLoader` im Objekt `ModelLoader` mit dem UI-Canvas (FileBrowserUI, ToastPresenter) und passe bei
   Bedarf den `ContentRoot` an.
5. Optional: Teleportation Areas hinzufügen, um sich um die geladenen Modelle zu bewegen.
