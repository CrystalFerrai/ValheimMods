## 2.2.3

* Updated mod package and documentation. No functional changes.

## 2.2.2

* Update to ConditionalConfigSync 1.0.6 which supports server-controlled configuration for requiring the mod on clients.
* Removed `ModRequired` config value. The new ConditionalConfigSync.ModRequirements.cfg file in the ConditionalConfigSync config replaces this feature.

## 2.2.1

* Added `ModRequired` config value. If set to true on a server (default), connecting clients must have the mod installed. If false, the mod is optional for clients. Mod features may not work reliably if any clients do not have the mod installed.

## 2.2.0

* Added server config sync via [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync)

## 2.1.0

* Updated for Valheim 1.0
* Min and max explore radius is now configurable.

## 2.0.13

* Updated BepinEx version

## 2.0.12

* Updated for game compatibility

## 2.0.11

* Updated BepinEx version
* Updated .NET version

## 2.0.10

* Updated BepinEx version

## 2.0.9

* Updated BepinEx version

## 2.0.8

* Fixed compatibility issue with new game version.

## 2.0.7

* Added an option to display exploration radius calculation variables on the screen (in case you want to know why the radius is what it is at a given moment).

## 2.0.6

* Updated BepInEx version

## 2.0.5

* The radius value displayed on screen (if that option is enabled) is now correct when in a dungeon.

## 2.0.4

* Updated for compatibility with game update
* Updated BepInEx version

## 2.0.3

* Accidentally built 2.0.2 with debug info. This removes that.

## 2.0.2

* Small fix for 2.0.1 change to ensure forest penalty multiplier is accounted for when decreasing altitude bonus.

## 2.0.1

* Decrease altitude bonus while in a forest.

## 2.0.0

* Complete overhaul of radius calculation.
    * Weather now affects the radius
    * Daylight amount now affects the radius
* Some config options have been added, removed, or had their default values changed. For the best experience, it is recommended that you delete your config from before 2.0.0 and allow the game to generate a new one.
* It is now possible to display the currently calculated exploration radius on the Hud to help with tweaking config values.

## 1.0.2

* Changing the mod config live (via something like BepInEx Configuration Manager) is now supported.

## 1.0.1

* Fixed bug where entering a dungeon would reveal a large map radius due to dungeons being way up in the sky.

## 1.0.0

* Initial release
