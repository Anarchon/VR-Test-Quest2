# SampleLoaderScene Setup

1. Öffne `Assets/Scenes/SampleLoaderScene.unity`. Sie enthält bereits ein `ContentRoot`, das vor der Kamera platziert ist, sowie
   den `ModelLoader` mit referenziertem `DefaultModel`-Material.
2. Stelle sicher, dass im Package Manager das `XR Interaction Toolkit` in Version 2.5.4 installiert ist (Manifest-Eintrag vorhanden).
3. Ersetze die `Main Camera` durch ein `XR Origin (Action Based)` Prefab aus dem XR Interaction Toolkit und aktiviere Ray- plus
   Direct-Interactor.
4. Importiere die **XRI Default Input Actions** (Package Manager > XR Interaction Toolkit > Samples) und verknüpfe das erzeugte
   Input Action Asset mit dem `XR Origin`/den Action-Based Controller Komponenten, damit Quest Touch Controller inklusive
   Teleport/Grab/Lokomotion funktionieren.
5. Platziere einen World-Space-Canvas (ca. 0.8m vor dem Startpunkt) und weise das Skript `FileBrowserUI` zu.
   - Dropdown/Text/Button-Referenzen per Drag & Drop setzen.
   - `ToastPresenter` auf demselben Canvas platzieren (CanvasGroup + Text).
6. Verbinde den `RuntimeModelLoader` im Objekt `ModelLoader` mit dem UI-Canvas (FileBrowserUI, ToastPresenter) und passe bei
   Bedarf den `ContentRoot` an.
7. Lege im Build/auf dem Gerät einen Ordner `Models` im Spielverzeichnis (bzw. `Application.persistentDataPath/Models`) an und
   lege pro Modell einen Unterordner mit der eigentlichen Datei ab (z. B. `Models/Car/car.glb`). Optional kann derselbe Aufbau
   in `StreamingAssets/Models` für mitgelieferte Beispiele verwendet werden.
8. Optional: Teleportation Areas hinzufügen, um sich um die geladenen Modelle zu bewegen.
9. Für Builds: `Scripting Backend` auf IL2CPP und Zielarchitektur `ARM64` kontrollieren (ProjectSettings sind bereits
   vorkonfiguriert), damit Quest-Deployments 64-Bit-Anforderungen erfüllen.
