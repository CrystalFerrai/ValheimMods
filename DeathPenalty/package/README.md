Allows tweaking various death related values. You can make death softer or harder as desired by editing the config.

* Tune the death penalty skill level loss percentage.
* Toggle whether skill progress towards the next level is reset.
* Modify the duration of the "No Skill Loss" buff.
* Modify the duration of the "Corpse Run" buff.

Run the game once with the mod enabled to generate the config. See config for details on what each option does.

This mod must be installed on client and server.

Some configuration options can be enforced by a server running [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync). See included ConfigSync_Readme.txt file for more information.

## Installation
This mod is designed to install and run via a mod manager such as [r2modman](https://thunderstore.io/package/ebkr/r2modman/). You can optionally install it manually following the steps below.

**Manual Install**

1. Install [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Install [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync)
3. Download latest ``DeathPenalty.dll`` by clicking "Manual Download". Extract the dll from the zip file into ``[GameDirectory]\Bepinex\plugins``. (You only need the dll.)
4. Run the game once, then close it and edit the generated cfg file in ``[GameDirectory]\Bepinex\config`` if you want to customize anything (or use a configuration management mod).

## Changelog

1.3.0

* Added server config sync via [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync)

1.2.0

* Updated for Valheim 1.0

1.1.4

* Updated BepinEx version

1.1.3

* Updated BepinEx version
* Updated .NET version

1.1.2

* Updated BepinEx version

1.1.1

* Updated BepinEx version

1.1.0

* Added option to disable losing progress towards next level.

1.0.4

* Updated BepInEx version

1.0.3

* Updated BepInEx version

1.0.2

* Changing the mod config live (via something like BepInEx Configuration Manager) is now supported.

1.0.1

* Clamped config values to prevent breaking things.

1.0.0

* Initial release
