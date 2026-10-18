# Anno 1800 Map Editor (Modded by gz2k2)

Welcome to the modded version of the Anno 1800 Map Editor by **gz2k2**. This version allows you to create, manage, and export custom maps with ease, including support for additional custom assets like material sets and graphics.
*Fork of [anno-mods/AnnoMapEditor](https://github.com)*

## How to Include Custom Maps

### Configuration
To use custom maps in the editor, you must first configure the **path inside the settings menu (gear icon)**. Once set up, your custom maps will be visible and listed directly within that menu.

### Directory Structure Example

Your custom map files must be placed inside a dedicated subfolder within the configured directory:

```text
/maps/
└── my_custom_maps/
    ├── moderate_l_01_skinned.a7m
    └── additional_files/
        └── data/
            ├── config/
            │   └── engine/
            │       └── material_sets/
            │           └── xxx.xml
            └── graphics/
                └── props/
                    └── your_custom_prop.cfg
```

### Adding Additional Files (Auto-Export)

Any files and folders placed inside the `additional_files` directory will be **automatically copied** into your final map folder during the export process. This is useful for bundling custom textures, properties, or configurations with your map.

* **Material Sets Example:** 
  Place your custom material XML files here:
  `/maps/my_custom_maps/additional_files/data/config/engine/material_sets/xxx.xml`

* **Graphics & Props Example:** 
  Place your custom 3D models, textures, or prop configurations here:
  `/maps/my_custom_maps/additional_files/data/graphics/props/...`

## Fixes & Improvements (Compared to Original Version)

* **New World Map Export:** The export functionality for the New World map template now works completely without errors.
* **Copy & Paste Functionality:** The copy and paste buttons are fully active. You can now duplicate and insert islands directly.
* **Fertility Randomizer:** Added a dedicated, separate fertility randomizer designed specifically for custom maps.

## General Features
* Support for custom subfolder organization.
* Automatic asset bundling via the `additional_files` directory.
* Tailored for advanced Anno 1800 map modding.
