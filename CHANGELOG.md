## Anno 1800 Map Editor modified by gz2k2

### v0.7.87

#### Settings
- Reworked the **Settings** window into a dark-themed tab navigation.
- Added a **Folders** tab for the **Anno Game Path** and **Default template folder** settings.
- Added a **Map packages** tab containing the package table and the **Select map folder** / **Select files folder** controls.
- Added an **Anno Game Path** folder picker.
- Added the **GUID Management** tab.
- GUID Management now uses a single numeric **GUID** base value stored in `config.json`.
- Added an explanation that up to 25 GUIDs are reserved per custom island map.
- Added a red warning explaining that custom, unused GUIDs are mandatory and that incorrect or duplicate GUIDs can cause errors in the game.

#### Custom islands
- Added a dedicated **Select Custom Island** overlay for adding custom islands.
- The toolbar **Add Custom Island (+)** button now opens the custom-island overlay directly instead of using the old package context menu.
- Added a package-name dropdown to the custom-island overlay. Selecting a package filters the displayed islands to that package.
- Custom islands are displayed as image tiles with their map names and are sorted alphabetically by map name.
- **Island Type** is now available under **Fixed Island Properties** for custom islands, allowing `Normal` and `Starter` selection.
- Changed the **App randomizes xxx** label to black text for improved readability on the warning background.

#### Custom island export
- Added automatic `RandomIsland` asset definitions for custom islands to the exported `assets.xml`.
- The custom-island asset uses the same virtual `.a7m` path that is used when the island files are copied into the exported mod.
- Custom-island `RandomIsland` assets use `Normal;Hard` island difficulties and `Normal;Starter` island types.
- Added session-specific `IslandRegion` mapping:
  - The Old World -> `Moderate`
  - The New World -> `Colony01`
  - Cape Trelawney -> `Moderate`
  - The Arctic -> `Arctic`
  - Enbesa -> `Africa`
- Added session-specific GUID pools with 25 GUIDs per session, based on the GUID configured under **Settings > GUID Management**:
  - The Old World: base GUID + `0-24`
  - The New World: base GUID + `25-49`
  - Cape Trelawney: base GUID + `50-74`
  - The Arctic: base GUID + `75-99`
  - Enbesa: base GUID + `100-124`
- The exporter searches each session range for free GUIDs and avoids GUIDs already present in the loaded game assets or an existing package `assets.xml`.
- Exporting fails with a descriptive error if the base GUID is missing/invalid, the session is unsupported, or the session's GUID range has no sufficient free GUIDs.
- Existing package-provided `assets.xml` ModOps are preserved and merged with the map-template and generated custom-island ModOps instead of being overwritten.
- Detailed export errors are now displayed in the export dialog instead of only showing a generic failure message.
