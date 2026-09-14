## 1.4.4

* Mod is no longer required by default for all clients on a server since it should only affect players who are using it. This can be overridden by a server admin via ConditionalConfigSync.ModRequirements.cfg if requiring the mod is desired.
* Updated mod package and documentation.

## 1.4.3

* Fix stamina use multiplier not applying correctly.

## 1.4.2

* Update to ConditionalConfigSync 1.0.6 which supports server-controlled configuration for requiring the mod on clients.
* Removed `ModRequired` config value. The new ConditionalConfigSync.ModRequirements.cfg file in the ConditionalConfigSync config replaces this feature.

## 1.4.1

* Added `ModRequired` config value. If set to true on a server (default), connecting clients must have the mod installed. If false, the mod is optional for clients. Mod features may not work reliably if any clients do not have the mod installed.

## 1.4.0

* Added server config sync via [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync)

## 1.3.0

* Updated for Valheim 1.0

## 1.2.3

* Updated BepinEx version

## 1.2.2

* Updated BepinEx version
* Updated .NET version

## 1.2.1

* Updated BepinEx version

## 1.2.0

* New config option to change the stamina cost of using placement tools.

## 1.1.3

* Updated BepinEx version

## 1.1.2

* Updated BepInEx version

## 1.1.1

* Updated BepInEx version

## 1.1.0

* Accounted for changes in game patch.
* Place and remove delays are now separately configurable values.

## 1.0.3

* Added a min and max to ToolUseDelay.

## 1.0.2

* Changing the mod config live (via something like BepInEx Configuration Manager) is now supported.
* ToolUseDelay config option now defaults to 0.25 seconds instead of 0 (game default is 0.5).

## 1.0.1

* Version 1.0.0 was not uploaded properly.

## 1.0.0

* Initial release
