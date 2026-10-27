# Anno 1800 Map Editor modified by gz2k2

## v0.7.59

### Custom map packages

- The former single **Custom-island folder** setting has been replaced by map packages.
- A map package contains:
  - a package name;
  - a **Custom Map Folder** containing one or more custom-island folders and their `.a7m` / `.a7minfo` files;
  - a **Custom Files Folder** containing shared files that must be included with a map export.
- Packages are managed in the Settings window. Use **+ New package** to add one, edit the values directly in the table, choose folders for the selected row, or use **Delete selected** to remove it.
- Package rows are shown alphabetically by package name.
- The **Add custom island** menu is grouped by package name; each group contains that package's available islands.
- When an exported map uses an island from a package, the package's Custom Files Folder is copied into the root of the exported mod. If a map uses islands from several packages, the files of every involved package are included.
- Custom islands are exported under `data\ame\custom\<package-id>\...` to prevent collisions between packages with similarly named islands.

### Settings

- The toolbar gear button now opens a dedicated Settings window instead of a small popup.
- Settings are stored in `config.json` beside `AnnoMapEditor.exe`.
- Existing legacy user settings are imported automatically the first time no `config.json` exists.
- The old single custom-island path is migrated to a package named `Migrated package`.

### Interface

- The map-package table follows the application's dark theme, including its header, cells, selection, hover state, grid lines, and row header.

### Export

- Custom islands no longer export to `data\ame\sessions\islands\pool\custom`.
- They now export to `data\ame\custom\...`.
