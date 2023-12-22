This mod adjusts some things I don't like about portals.

* Anything can be carried through portals.
* The loading screen for teleporting is shortened when possible.
* Slightly decreased the distance from a portal a player needs to be to make it light up and make noise.

The various features can be toggled/tuned. Run the game once with the mod enabled to generate the config. See config for details on what each option does.

This mod is client only and does not need to be installed on dedicated servers. For the best experience, all clients should use the same configuration.

## Installation
This mod is designed to install and run via [r2modman](https://thunderstore.io/package/ebkr/r2modman/). You can optionally install it manually following the steps below.

**Manual Install**
1. Install [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Download latest ``ProperPortals.dll`` by clicking "Manual Download". Extract the dll from the zip file into ``[GameDirectory]\Bepinex\plugins``. (You only need the dll.)
3. Run the game once, then close it and edit the generated cfg file in ``[GameDirectory]\Bepinex\config`` if you want to customize anything.

## Changelog

1.2.5

* Updated BepinEx version
* Updated .NET version

1.2.4

* Updated BepinEx version

1.2.3

* Updated BepinEx version

1.2.2

* Updated BepInEx version

1.2.1

* Updated BepInEx version

1.2.0

* Decrease portal activation range and add a setting to configure it.

1.1.0

* Shorter screen fade time prior to teleport, configurable.
* Shortened the "fix bad position" timeout so that teleport mods which place the player at a bad Y position don't negate the effect of ths mod.

1.0.2

* Changed MinPortalTime default value from 0 to 1 because anything less than 1 looks bad in game due to the 1 second fade out.

1.0.1

* Changing the mod config live (via something like BepInEx Configuration Manager) is now supported.

1.0.0

* Initial release
