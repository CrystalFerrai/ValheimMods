Reduced usage delay for build hammer and other placement tools. Delay is configurable.

Run the game once with the mod enabled to generate the config. See config for details on what each option does.

## Installation
This mod is designed to install and run via [r2modman](https://thunderstore.io/package/ebkr/r2modman/). You can optionally install it manually following the steps below.

**Manual Install**
1. Install [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Download latest ``FastTools.dll`` by clicking "Manual Download". Extract the dll from the zip file to into ``[GameDirectory]\Bepinex\plugins``. (You only need the dll.)
3. Run the game once, then close it and edit the generated cfg file in ``[GameDirectory]\Bepinex\config`` if you want to customize anything.

## Changelog
1.1.0

* Accounted for changes in game patch.
* Place and remove delays are now separately configurable values.

1.0.3

* Added a min and max to ToolUseDelay.

1.0.2

* Changing the mod config live (via something like BepInEx Configuration Manager) is now supported.
* ToolUseDelay config option now defaults to 0.25 seconds instead of 0 (game default is 0.5).

1.0.1

* Version 1.0.0 was not uploaded properly.

1.0.0

* Initial release
