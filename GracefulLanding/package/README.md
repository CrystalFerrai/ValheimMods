Reduces fall damage.

This is a very simple mod that replaces a few fall damage related values with configurable values. By default, you can fall twice as far before taking any damage and four times as far before taking max damage.

Run the game once with the mod enabled to generate the config. See config for details on what each option does.

This mod should be installed on client and server. Mod features may not work reliably if any clients do not have the mod installed. By default, a server running the mod will reject clients that do not have it installed. To allow unmodded clients, at the risk of unreliable functionality, a server may set `ModRequired`=`false` in the config.

Some configuration options can be enforced by a server running [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync). See included ConfigSync_Readme.txt file for more information.

## Installation
This mod is designed to install and run via a mod manager such as [r2modman](https://thunderstore.io/package/ebkr/r2modman/). You can optionally install it manually following the steps below.

**Manual Install**

1. Install [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Install [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync)
3. Download latest ``GracefulLanding.dll`` by clicking "Manual Download". Extract the dll from the zip file into ``[GameDirectory]\Bepinex\plugins``. (You only need the dll.)
4. Run the game once, then close it and edit the generated cfg file in ``[GameDirectory]\Bepinex\config`` if you want to customize anything (or use a configuration management mod).

## Changelog

1.2.1

* Added `ModRequired` config value. If set to true on a server (default), connecting clients must have the mod installed. If false, the mod is optional for clients. Mod features may not work reliably if any clients do not have the mod installed.

1.2.0

* Added server config sync via [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync)

1.1.0

* Updated for Valheim 1.0

1.0.7

* Updated BepinEx version

1.0.6

* Updated BepinEx version
* Updated .NET version

1.0.5

* Updated BepinEx version

1.0.4

* Updated BepinEx version

1.0.3

* Updated BepInEx version

1.0.2

* Updated BepInEx version

1.0.1

* Changing the mod config live (via something like BepInEx Configuration Manager) is now supported.

1.0.0

* Initial release
