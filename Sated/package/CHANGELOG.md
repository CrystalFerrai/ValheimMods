## 1.3.3

* Mod is no longer required by default for all clients on a server since it should only affect players who are using it. This can be overridden by a server admin via ConditionalConfigSync.ModRequirements.cfg if requiring the mod is desired.
* Updated mod package and documentation.

## 1.3.2

* Update to ConditionalConfigSync 1.0.6 which supports server-controlled configuration for requiring the mod on clients.
* Removed `ModRequired` config value. The new ConditionalConfigSync.ModRequirements.cfg file in the ConditionalConfigSync config replaces this feature.

## 1.3.1

* Added `ModRequired` config value. If set to true on a server (default), connecting clients must have the mod installed. If false, the mod is optional for clients. Mod features may not work reliably if any clients do not have the mod installed.

## 1.3.0

* Added server config sync via [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync)

## 1.2.0

* Updated for Valheim 1.0

## 1.1.12

* Updated BepinEx version

## 1.1.11

* Updated BepinEx version
* Updated .NET version

## 1.1.10

* Updated BepinEx version

## 1.1.9

* Updated BepinEx version

## 1.1.8

* Modified patching method for compatibility with Azumatt's MagicEitrBase mod.

## 1.1.7

* Fixed an issue preventing food from providing Eitr.

## 1.1.6

* Updated BepInEx version

## 1.1.5

* Fixed the time input to the curve function resulting in a proper drop off near the end. (This stopped working at some point due to a change to the vanilla formula.)

## 1.1.4

* Fixed some errors in total health and stamina calculations.

## 1.1.3

* Removed the food timer bar feature because it breaks the UI after the latest game update.
* Updated BepInEx version

## 1.1.2

* Changing the mod config live (via something like BepInEx Configuration Manager) is now supported.

## 1.1.1

* Added config options to adjust the health and stamina food curve exponents.

## 1.1.0

* Food icons now have timer bars below them (can be disabled).

## 1.0.2

* The food bar on the HUD now properly matches the HP bar.

## 1.0.1

* Bad upload. This version contained the 1.0.0 dll.

## 1.0.0

* Initial release
