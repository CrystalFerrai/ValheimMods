Reduced usage delay for build hammer and other placement tools. Delay is configurable. Stamina cost is also configurable.

Run the game once with the mod enabled to generate the config. See config for details on what each option does.

This mod must be installed on client and server.

Some configuration options can be enforced by a server running [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync). See included ConfigSync_Readme.txt file for more information.

## Installation
This mod is designed to install and run via a mod manager such as [r2modman](https://thunderstore.io/package/ebkr/r2modman/). You can optionally install it manually following the steps below.

**Manual Install**

1. Install [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Install [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync)
3. Download latest ``FastTools.dll`` by clicking "Manual Download". Extract the dll from the zip file into ``[GameDirectory]\Bepinex\plugins``. (You only need the dll.)
4. Run the game once, then close it and edit the generated cfg file in ``[GameDirectory]\Bepinex\config`` if you want to customize anything (or use a configuration management mod).

## Changelog

1.4.0

* Added server config sync via [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync)

1.3.0

* Updated for Valheim 1.0

1.2.3

* Updated BepinEx version

1.2.2

* Updated BepinEx version
* Updated .NET version

1.2.1

* Updated BepinEx version

1.2.0

* New config option to change the stamina cost of using placement tools.

1.1.3

* Updated BepinEx version

1.1.2

* Updated BepInEx version

1.1.1

* Updated BepInEx version

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
