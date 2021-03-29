Increases the base map discovery range around the player as well as dynamically adjusts the range based on various visibility factors.

* The base discovery radius is doubled compared to vanilla.
* Discovery radius while on a boat is tripled compared to vanilla.
* Radius increases gradually based on altitude.
* Radius increased by 20% of base during daylight.
* Radius decreased by 10% of base while in a forested area.

Values listed above are defaults. All of them are configurable. Run the game once with the mod enabled to generate the config. See config for details on what each option does.

Plan to add weather based range modifiers in the future.

## Installation
This mod is designed to install and run via [r2modman](https://thunderstore.io/package/ebkr/r2modman/). You can optionally install it manually following the steps below.

**Manual Install**
1. Install [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Download latest ``Pathfinder.dll`` by clicking "Manual Download". Extract the dll from the zip file into ``[GameDirectory]\Bepinex\plugins``. (You only need the dll.)
3. Run the game once, then close it and edit the generated cfg file in ``[GameDirectory]\Bepinex\config`` if you want to customize anything.

## Changelog
1.0.2

* Changing the mod config live (via something like BepInEx Configuration Manager) is now supported.

1.0.1

* Fixed bug where entering a dungeon would reveal a large map radius due to dungeons being way up in the sky.

1.0.0

* Initial release
