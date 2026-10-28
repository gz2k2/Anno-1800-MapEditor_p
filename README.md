# Anno 1800 Map Editor (Modded by gz2k2)

Welcome to the modified **Anno 1800 Map Editor** by **gz2k2**.

This fork extends the original editor with workflows for custom islands, map packages, additional mod files, custom-island export integration, improved island configuration, and other quality-of-life features.

> This project is a fork of [anno-mods/AnnoMapEditor](https://github.com/anno-mods/AnnoMapEditor).

---

## Custom Islands

Custom islands are external `.a7m` maps that can be loaded into the editor, placed on a map template, configured like fixed islands, and exported together with the resulting map mod.

The editor manages custom islands through **Map Packages**. A package keeps the custom island maps and any additional files that belong to those maps together.

### Required custom island files

A custom island is discovered from its `.a7m` file. A matching `.a7minfo` file must be available beside it so that the editor can read the island metadata.

Example:

```text
MyCustomIslands/
└── moderate_l_01_skin/
    ├── moderate_l_01_skin.a7m
    ├── moderate_l_01_skin.a7minfo
    └── _gamedata/
        └── ...
```

The `_gamedata` directory and other files located in the island directory are treated as part of the custom island and are copied when the island is required by an exported map.

Custom-map folders are searched recursively. This means islands can be organized into subfolders such as regions, authors, projects, or map collections.

---

## Adding a Custom Island to a Map

Use the **Add Custom Island (+)** button in the main toolbar.

The button opens the dedicated **Select Custom Island** window instead of mixing custom islands into the normal Fixed Island selection.

### Package filter

The dropdown at the top of the window lists the available **Map Package names**.

Selecting a package filters the island list so that only maps belonging to that package are displayed.

### Island browser

The lower part of the window displays custom islands as image tiles:

- the island preview is displayed when a thumbnail is available;
- the map name is shown beneath the image;
- maps are sorted alphabetically by map name.

Select an island tile to add the custom island to the current map.

Custom islands are deliberately excluded from the normal **Fixed Island** picker. The Fixed Island picker therefore remains reserved for original islands loaded from the Anno 1800 game data / RDA archives.

---

## Custom Island Properties

Once a custom island has been added, it uses the existing **Fixed Island Properties** editor.

Custom islands additionally expose **Island Type**, where the island can be configured as:

- `Normal`
- `Starter`

The existing fixed-island fertility and slot configuration controls can also be used with custom islands.

For maps where the game cannot reliably assign region-compatible resources itself, the editor provides its application-side fertility and slot randomization options.

---

# Settings

Open Settings with the **gear icon** in the toolbar.

The Settings window is divided into tabs. All persistent application settings are stored in `config.json` beside `AnnoMapEditor.exe`.

The **Close** button remains available regardless of which Settings tab is selected.

---

## Settings: Folders

The **Folders** tab contains general paths used by the application.

### Anno Game Path

Select the installation directory of **Anno 1800**.

The editor uses this location to derive paths required for loading the game's data and mods.

### Default template folder

Defines the default folder used when working with map template files.

---

## Settings: Map Packages

The **Map packages** tab manages collections of custom islands and their associated export files.

Each row represents one package and contains:

- **Package name**
- **Custom Map Folder**
- **Custom Files Folder**

Use **+ New package** to create another package and **Delete selected** to remove the selected entry.

### Package name

The package name is the human-readable name shown in the package dropdown of the **Select Custom Island** window.

Use a clear name such as:

```text
White and Cold Islands
Arctic Reskins
My Custom Maps
```

### Custom Map Folder

The **Custom Map Folder** contains the actual custom islands.

The editor searches this directory recursively for `.a7m` files and their matching `.a7minfo` files.

Example:

```text
CustomMaps/
├── OldWorld/
│   └── moderate_l_01_skin/
│       ├── moderate_l_01_skin.a7m
│       └── moderate_l_01_skin.a7minfo
└── Arctic/
    └── colony03_custom_01/
        ├── colony03_custom_01.a7m
        └── colony03_custom_01.a7minfo
```

Use **Select map folder...** to assign this directory to the selected package.

### Custom Files Folder

The **Custom Files Folder** is for shared files required by the package but not stored directly inside an individual custom-island directory.

Examples include:

- graphics;
- props;
- textures;
- material sets;
- configuration XML files;
- other data files required by the custom islands.

The directory should mirror the path structure expected inside the final mod.

Example:

```text
PackageFiles/
└── data/
    ├── config/
    │   └── engine/
    │       └── material_sets/
    │           └── custom_material.xml
    └── graphics/
        └── props/
            └── custom_prop.cfg
```

When an exported map uses at least one island belonging to that package, the package's Custom Files Folder is copied into the exported mod.

If multiple packages are used by the same map, the required files from all used packages are included.

Use **Select files folder...** to assign this directory to the selected package.

### Package IDs

Every map package receives a short two-digit internal ID:

```text
01
02
03
...
99
```

The ID is used in custom-island export paths to avoid collisions between packages while keeping paths short.

A typical exported custom-island path therefore looks like:

```text
data/ame/custom/01/.../my_custom_island.a7m
```

Existing old or invalid package IDs are migrated to available two-digit IDs. Duplicate IDs are also reassigned automatically.

A maximum of 99 map packages is supported.

---

## Settings: GUID Management

The **GUID Management** tab defines the base GUID used when exporting custom islands.

Only numeric values can be entered in the GUID field.

### Why custom islands need GUIDs

A custom island that is used by an exported map also has to be registered as a `RandomIsland` asset in the exported `assets.xml`.

This registration allows the game to resolve the custom island as an asset so that features such as fertilities and slots can be assigned correctly.

The editor automatically creates these `RandomIsland` entries during map export.

### GUID allocation

The value entered under **GUID Management > GUID** is the beginning of the editor's reserved custom-island GUID space.

Each supported session receives a dedicated range of **25 GUIDs**.

For example, with a base GUID of `1000`:

```text
The Old World:   1000 - 1024
The New World:   1025 - 1049
Cape Trelawney:  1050 - 1074
The Arctic:      1075 - 1099
Enbesa:          1100 - 1124
```

In general:

```text
The Old World:   base +   0 to base +  24
The New World:   base +  25 to base +  49
Cape Trelawney:  base +  50 to base +  74
The Arctic:      base +  75 to base +  99
Enbesa:          base + 100 to base + 124
```

When exporting, the editor searches the relevant range for a free GUID.

GUIDs already found in the loaded game assets or in an existing package-provided `assets.xml` are skipped.

If the selected session does not have enough free GUIDs for all custom islands used by the map, export is stopped and an error is displayed.

> **WARNING! It is essential and mandatory to use your own unused GUIDs. Using incorrect or duplicate GUIDs can and will cause errors in the game.**

Do not copy GUID ranges from another mod. Choose a GUID range that is reserved for your own project and does not overlap with assets from other installed mods.

---

# Custom Island Export

When a map containing custom islands is exported, the editor handles both the physical island files and the required asset registration.

## Island files

The complete custom-island directory is copied into the exported mod under a package-specific virtual path similar to:

```text
data/ame/custom/01/maps/my_custom_island/my_custom_island.a7m
```

The exact virtual path stored for the island is also used for the generated `assets.xml` entry. This keeps the copied `.a7m` file and its `RandomIsland` asset definition synchronized.

## assets.xml integration

Each unique custom island used by the map receives a generated `RandomIsland` asset definition.

Conceptually, the generated entry follows this structure:

```xml
<ModOp GUID="116676" Type="addNextSibling">
  <Asset>
    <Template>RandomIsland</Template>
    <Values>
      <Standard>
        <GUID>...</GUID>
        <Name>my_custom_island</Name>
      </Standard>
      <RandomIsland>
        <FilePath>data/ame/custom/01/.../my_custom_island.a7m</FilePath>
        <IslandRegion>Moderate</IslandRegion>
        <IslandDifficulty>Normal;Hard</IslandDifficulty>
        <IslandType>Normal;Starter</IslandType>
      </RandomIsland>
    </Values>
  </Asset>
</ModOp>
```

The GUID, map name, file path, and island region are generated dynamically.

### Region mapping

The editor translates the exported map session to the required `IslandRegion` value:

```text
The Old World  -> Moderate
The New World  -> Colony01
Cape Trelawney -> Moderate
The Arctic     -> Arctic
Enbesa         -> Africa
```

### Existing package assets.xml

A package's **Custom Files Folder** may already contain an `assets.xml` with custom ModOps.

The exporter preserves those existing entries and adds the map-template and generated custom-island operations instead of intentionally replacing the package content.

---

# Recommended Custom Island Workflow

1. Open **Settings**.
2. Under **Folders**, verify the Anno Game Path.
3. Open **Map packages**.
4. Create a package with **+ New package**.
5. Give the package a recognizable name.
6. Assign the package's **Custom Map Folder**.
7. If required, assign a **Custom Files Folder**.
8. Open **GUID Management**.
9. Enter a GUID base range reserved exclusively for your own project.
10. Close Settings.
11. Open or create the map template you want to edit.
12. Click **Add Custom Island (+)**.
13. Select the package from the dropdown.
14. Select the desired custom island from the image list.
15. Configure the island under **Fixed Island Properties** as required.
16. Export the map as a mod.
17. The editor copies the required island/package files and generates the corresponding `RandomIsland` entries in `assets.xml`.

---

# Fixes & Improvements Compared to the Original Editor

- **Custom islands:** Package-based custom island discovery, selection, configuration, and export.
- **New World map export:** Improved New World map-template export support.
- **Copy & Paste:** Island copy and paste functionality is enabled.
- **Fertility randomization:** Additional editor-side fertility handling for custom and cross-region islands.
- **Slot randomization:** Editor-side resource assignment is available where the game's normal randomization is insufficient.
- **Map packages:** Package-specific maps and additional mod files can be managed separately.
- **GUID management:** Automatic custom-island asset registration using configurable GUID ranges.

---

# Notes

This project is not affiliated with Ubisoft.

Anno 1800 and Anno are trademarks of their respective owners.
