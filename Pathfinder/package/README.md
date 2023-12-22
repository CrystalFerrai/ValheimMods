Increases the base map discovery range around the player as well as dynamically adjusts the range based on various visibility factors.

* The discovery radius is generally increased compared to vanilla.
* Discovery radius is further increased while on a boat.
* Radius increases gradually based on altitude.
* Radius increases or decreases based on current amount of daylight.
* Radius can be decreased by weather effects such as fog, rain or snow.
* Radius decreased by 30% while in a forested area.

Values listed above are defaults. All of them are configurable. Run the game once with the mod enabled to generate the config. See config for details on what each option does.

It is generally recommended to only adjust the radius values in the "Base" category of the config. Default values in the "Multipliers" category have been tweaked to try to approximate actual visibility changes, and changing them can significantly impact exploration radius in sometimes unexpected ways.

This mod is client only and does not need to be installed on dedicated servers. For the best experience, all clients should use the same configuration.

## Installation
This mod is designed to install and run via [r2modman](https://thunderstore.io/package/ebkr/r2modman/). You can optionally install it manually following the steps below.

**Manual Install**
1. Install [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Download latest ``Pathfinder.dll`` by clicking "Manual Download". Extract the dll from the zip file into ``[GameDirectory]\Bepinex\plugins``. (You only need the dll.)
3. Run the game once, then close it and edit the generated cfg file in ``[GameDirectory]\Bepinex\config`` if you want to customize anything.

## Changelog

2.0.11

* Updated BepinEx version
* Updated .NET version

2.0.10

* Updated BepinEx version

2.0.9

* Updated BepinEx version

2.0.8

* Fixed compatibility issue with new game version.

2.0.7

* Added an option to display exploration radius calculation variables on the screen (in case you want to know why the radius is what it is at a given moment).

2.0.6

* Updated BepInEx version

2.0.5

* The radius value displayed on screen (if that option is enabled) is now correct when in a dungeon.

2.0.4

* Updated for compatibility with game update
* Updated BepInEx version

2.0.3

* Accidentally built 2.0.2 with debug info. This removes that.

2.0.2

* Small fix for 2.0.1 change to ensure forest penalty multiplier is accounted for when decreasing altitude bonus.

2.0.1

* Decrease altitude bonus while in a forest.

2.0.0

* Complete overhaul of radius calculation.
    * Weather now affects the radius
    * Daylight amount now affects the radius
* Some config options have been added, removed, or had their default values changed. For the best experience, it is recommended that you delete your config from before 2.0.0 and allow the game to generate a new one.
* It is now possible to display the currently calculated exploration radius on the Hud to help with tweaking config values.

1.0.2

* Changing the mod config live (via something like BepInEx Configuration Manager) is now supported.

1.0.1

* Fixed bug where entering a dungeon would reveal a large map radius due to dungeons being way up in the sky.

1.0.0

* Initial release
