# Quest 2 Runtime Model Loader (Unity XR Interaction Toolkit)

Dieses Repository enthält ein Unity-Projektgerüst für die Meta Quest 2 (Oculus Quest 2), das OBJ-, STL- und glTF/GLB-Modelle zur Laufzeit lädt, im VR-Raum darstellt und per XR Interaction Toolkit manipulieren lässt. Das Setup ist für Unity 2022 LTS (Android / OpenXR) ausgelegt.

## Features
- OpenXR + Meta Quest 2 XR Rig (XR Origin) mit Ray Interactors und Direct Interactors
- Laden von OBJ-, STL- und glTF/GLB-Dateien aus `Application.persistentDataPath/Models` sowie optional `StreamingAssets/Models`
- Umwandlung der Geometriedaten in Unity Meshes
- Platzierung, Skalierung und Rotation der geladenen Modelle über XR Grab Interactable
- World-Space-UI für Dateiauswahl, Laden/Entfernen und Fehlermeldungen
- Sauber getrennte Loader-Logik und VR-Interaktionskomponenten

## Projektstruktur
```
Assets/
  Materials/
    DefaultModel.mat                 # Standard-Material für importierte Meshes
  Scenes/
    SampleLoaderScene.unity          # Beispielszene mit Loader/ContentRoot
    SceneSetup.md                    # Schritt-für-Schritt-Aufbau der Szene
  Scripts/
    Loaders/
      ObjImporter.cs                 # Einfacher OBJ-Parser (Triangles)
      StlImporter.cs                 # ASCII/Binary STL-Parser
      GltfImporter.cs                # glTF/GLB-Import via glTFast (Texturen inkl.)
      RuntimeModelLoader.cs          # Koordiniert Dateiwahl und Instanziierung
    Interaction/
      ModelPlacementController.cs    # Skaliert/rotiert per XR Interactors
      ModelMetadata.cs               # Kleine Info-Komponente pro geladenem Modell
    UI/
      FileBrowserUI.cs               # World-Space UI für Dateiliste und Aktionen
      ToastPresenter.cs              # Einfaches Fehlermeldungs-Popup
Packages/
  manifest.json                      # Vorgezogene XR-/Input-/URP-Abhängigkeiten
ProjectSettings/
  ProjectVersion.txt                 # Unity Version 2022.3.10f1
  EditorBuildSettings.asset          # Bindet SampleLoaderScene in die Buildliste ein
```

## Einrichtung in Unity (Schritt für Schritt)
1. **Unity Version**: Unity 2022 LTS (z. B. 2022.3.x). Plattform "Android" wählen.
2. **Pakete installieren**:
   - `XR Plugin Management`
   - `Oculus XR Plugin` (wird in OpenXR genutzt)
   - `XR Interaction Toolkit` (empfohlen: 2.5.4 für Unity 2022.3 LTS)
   - `XR Core Utils` **2.3.0** (Version, die von XRI 2.5.4 referenziert wird)
   - `glTFast` 6.1.0 (Manifest enthält `com.atteneder.gltfast` über den OpenUPM-Registry-Eintrag), um glTF/GLB inkl. Texturen zu laden
   - Eingebaute Module aktivieren: **Android JNI** (für `Permission/PermissionCallbacks`) und **Unity Analytics** (für `AnalyticsResult` in OpenXR)
   - `Input System` aktivieren, als Standard setzen **und** in den Player Settings die "Native Platform Backends for the New Input System" einschalten (erfordert Editor-Neustart).
   - Falls Unity den glTFast-Eintrag nicht findet: `Project Settings > Package Manager > Scoped Registries` öffnen und sicherstellen, dass `OpenUPM` mit Scope `com.atteneder` auf `https://package.openupm.com` eingetragen ist (siehe Manifest).
3. **OpenXR / Quest 2 aktivieren**:
   - `Edit > Project Settings > XR Plug-in Management`: Plattform `Android` auswählen, `OpenXR` anhaken.
   - Unter `OpenXR` -> `Features`: `Oculus Touch Controller Profile`, `Eye Gaze Interaction` (optional) aktivieren.
   - `Interaction Profiles`: Meta Quest Touch an oberste Stelle setzen.
4. **XR Origin in Szene**:
   - Öffne `Assets/Scenes/SampleLoaderScene.unity` und ersetze die `Main Camera` bei Bedarf durch ein `XR Origin (Action Based)` Prefab aus dem XR Interaction Toolkit.
   - Linke/rechte Hand-Controller (Ray Interactor + Direct Interactor) aktivieren.
   - `Teleportation Provider` + Teleportation Areas/Ways optional hinzufügen.
5. **UI platzieren**:
   - Ein World-Space-Canvas neben den Startpunkt stellen (Prefab aus `Assets/UI/` falls erstellt) und `FileBrowserUI` als Controller setzen.
   - Buttons `Refresh`, `Load Selected`, `Clear All`, `Load From StreamingAssets` verdrahten.
6. **Model Loader**:
   - In `SampleLoaderScene` ist bereits ein `ModelLoader`-Objekt vorhanden. Prüfe die Referenzen: `ContentRoot` (liegt im Raum vor dem Startpunkt) und `DefaultModel`-Material sind gesetzt.
   - Verknüpfe UI-Referenzen (FileBrowserUI/ToastPresenter), damit die Buttons funktionieren.
7. **Build-Einstellungen (Quest 2)**:
   - `File > Build Settings`: Plattform `Android`, `Texture Compression` ASTC, `Target API Level` 33 oder gemäß Meta-Richtlinien.
   - `Player Settings`: `Color Space` Linear, `Multithreaded Rendering` aktiviert, `Scripting Backend` IL2CPP, `ARM64` als Ziel-Architektur.
   - `Minimum API Level` >= 29, `Target` 33+, `Internet` Permission i. d. R. nicht nötig.
   - `XR Plug-in Management` sicherstellen, dass nur `OpenXR` aktiv ist.
8. **Dateien bereitstellen (Models-Ordner Pflicht)**:
   - Lege unter `Application.persistentDataPath/Models/` pro Modell einen eigenen Unterordner an (z. B. `Models/Spaceship/ship.glb`).
   - Modelle nach `Android/data/<APP-ID>/files/Models` pushen (`adb push myModelFolder /sdcard/Android/data/<APP-ID>/files/Models/`).
   - Alternativ in `Assets/StreamingAssets/Models/` legen, damit sie ins Build gepackt werden. StreamingAssets werden in der UI mit Präfix gekennzeichnet.
9. **Szenenspeichern**: Szene als `SampleLoaderScene` speichern und in den Build Settings hinzufügen.

## Erweiterung um neue Dateiformate
- Ergänze einen neuen Parser (z. B. `PlyImporter.cs`) im Ordner `Assets/Scripts/Loaders/`.
- Implementiere eine Methode `Mesh Parse(string path)` analog zu OBJ/STL **oder** eine Coroutine analog zu `GltfImporter.Load` für format-spezifische Pipelines.
- Registriere den Parser in `RuntimeModelLoader.SupportedExtensions` und ergänze die UI-Beschriftung.
- Optional: Material-/Texturzuweisungen im Parser vornehmen.

## Wichtige Hinweise
- Alle Skripte sind stark kommentiert, um die Verwendung in der Szene zu verdeutlichen.
- Für reale Produktionen empfehlen sich optimierte Loader (z. B. Assimp/UniGLTF) und Hintergrund-Threads für große Modelle.
- Der enthaltene OBJ-Parser erwartet triangulierte Modelle oder trianguliert per Fan-Methode.
