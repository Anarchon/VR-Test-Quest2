# Quest 2 Runtime Model Loader (Unity XR Interaction Toolkit)

Dieses Repository enthält ein Unity-Projektgerüst für die Meta Quest 2 (Oculus Quest 2), das OBJ- und STL-Modelle zur Laufzeit lädt, im VR-Raum darstellt und per XR Interaction Toolkit manipulieren lässt. Das Setup ist für Unity 2022 LTS (Android / OpenXR) ausgelegt.

## Features
- OpenXR + Meta Quest 2 XR Rig (XR Origin) mit Ray Interactors und Direct Interactors
- Laden von OBJ- und STL-Dateien aus `Application.persistentDataPath` sowie optional `StreamingAssets`
- Umwandlung der Geometriedaten in Unity Meshes
- Platzierung, Skalierung und Rotation der geladenen Modelle über XR Grab Interactable
- World-Space-UI für Dateiauswahl, Laden/Entfernen und Fehlermeldungen
- Sauber getrennte Loader-Logik und VR-Interaktionskomponenten

## Projektstruktur
```
Assets/
  Scenes/
    SampleLoaderScene.unity          # Beispiellandschaft mit XR Origin + UI
  Scripts/
    Loaders/
      ObjImporter.cs                # Einfacher OBJ-Parser (Triangles)
      StlImporter.cs                # ASCII/Binary STL-Parser
      RuntimeModelLoader.cs         # Koordiniert Dateiwahl und Instanziierung
    Interaction/
      ModelPlacementController.cs   # Skaliert/rotiert per XR Interactors
      ModelMetadata.cs              # Kleine Info-Komponente pro geladenem Modell
    UI/
      FileBrowserUI.cs              # World-Space UI für Dateiliste und Aktionen
      ToastPresenter.cs             # Einfaches Fehlermeldungs-Popup
```

## Einrichtung in Unity (Schritt für Schritt)
1. **Unity Version**: Unity 2022 LTS (z. B. 2022.3.x). Plattform "Android" wählen.
2. **Pakete installieren**:
   - `XR Plugin Management`
   - `Oculus XR Plugin` (wird in OpenXR genutzt)
   - `XR Interaction Toolkit` (2.4+)
   - `Input System` aktivieren und als Standard setzen.
3. **OpenXR / Quest 2 aktivieren**:
   - `Edit > Project Settings > XR Plug-in Management`: Plattform `Android` auswählen, `OpenXR` anhaken.
   - Unter `OpenXR` -> `Features`: `Oculus Touch Controller Profile`, `Eye Gaze Interaction` (optional) aktivieren.
   - `Interaction Profiles`: Meta Quest Touch an oberste Stelle setzen.
4. **XR Origin in Szene**:
   - Eine `XR Origin (Action Based)` aus dem XR Interaction Toolkit in die Szene ziehen.
   - Linke/rechte Hand-Controller (Ray Interactor + Direct Interactor) aktivieren.
   - `Teleportation Provider` + Teleportation Areas/Ways optional hinzufügen.
5. **UI platzieren**:
   - Ein World-Space-Canvas neben den Startpunkt stellen (Prefab aus `Assets/UI/` falls erstellt) und `FileBrowserUI` als Controller setzen.
   - Buttons `Refresh`, `Load Selected`, `Clear All`, `Load From StreamingAssets` verdrahten.
6. **Model Loader**:
   - Ein leeres GameObject `ModelLoader` in der Szene anlegen und `RuntimeModelLoader` hinzufügen.
   - Referenzen setzen: UI Canvas (ToastPresenter), `ContentRoot` (leeres Objekt für Instanzen), `DefaultMaterial` (z. B. Standard Lit) und optional `StreamingAssets Files` Toggle/Button.
7. **Build-Einstellungen (Quest 2)**:
   - `File > Build Settings`: Plattform `Android`, `Texture Compression` ASTC, `Target API Level` 33 oder gemäß Meta-Richtlinien.
   - `Player Settings`: `Color Space` Linear, `Multithreaded Rendering` aktiviert, `Scripting Backend` IL2CPP, `ARM64` als Ziel-Architektur.
   - `Minimum API Level` >= 29, `Target` 33+, `Internet` Permission i. d. R. nicht nötig.
   - `XR Plug-in Management` sicherstellen, dass nur `OpenXR` aktiv ist.
8. **Dateien bereitstellen**:
   - Modelle nach `Android/data/<APP-ID>/files` pushen (`adb push my.obj /sdcard/Android/data/<APP-ID>/files/`).
   - Alternativ in `Assets/StreamingAssets/` legen, damit sie ins Build gepackt werden.
9. **Szenenspeichern**: Szene als `SampleLoaderScene` speichern und in den Build Settings hinzufügen.

## Erweiterung um neue Dateiformate
- Ergänze einen neuen Parser (z. B. `PlyImporter.cs`) im Ordner `Assets/Scripts/Loaders/`.
- Implementiere eine Methode `Mesh Parse(string path)` analog zu OBJ/STL.
- Registriere den Parser in `RuntimeModelLoader.SupportedExtensions` und ergänze die UI-Beschriftung.
- Optional: Material-/Texturzuweisungen im Parser vornehmen.

## Wichtige Hinweise
- Alle Skripte sind stark kommentiert, um die Verwendung in der Szene zu verdeutlichen.
- Für reale Produktionen empfehlen sich optimierte Loader (z. B. Assimp/UniGLTF) und Hintergrund-Threads für große Modelle.
- Der enthaltene OBJ-Parser erwartet triangulierte Modelle oder trianguliert per Fan-Methode.
