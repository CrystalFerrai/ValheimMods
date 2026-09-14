## 1.2.4

* Updated mod package and documentation. No functional changes.

## 1.2.3

* Update to ConditionalConfigSync 1.0.6 which supports server-controlled configuration for requiring the mod on clients.
* Removed `ModRequired` config value. The new ConditionalConfigSync.ModRequirements.cfg file in the ConditionalConfigSync config replaces this feature.

## 1.2.2

* Added `ModRequired` config value. If set to true on a server, connecting clients must have the mod installed. If false (default), the mod is optional for clients.

## 1.2.1

* Restore camera water clip state if mod is disabled and reenabled via config
* Update CrystalLib to fix input key rebinding issue

## 1.2.0

* Added server config sync via [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync)

## 1.1.0

* Updated for Valheim 1.0

## 1.0.9

* Updated BepinEx version

## 1.0.8

* Updated CrystalLib version

## 1.0.7

* Updated BepinEx version
* Updated CrystalLib version
* Updated .NET version

## 1.0.6

* Updated BepinEx version
* Updated CrystalLib version

## 1.0.5

* Updated CrystalLib version

## 1.0.4

* Added dependency on CrystalLib and moved some code there.

## 1.0.3

* Fixed CameraIgnoreWater config option not applying properly.
* Fixed swim toggle keybind not working in multiplayer if there were an even number of players present.

## 1.0.2

* Updated BepinEx version

## 1.0.1

* The shortcut key for toggling player swimming can now be changed in the mod config.
* Renamed the setting "PlayerIgnoreWater" to "PlayerSwims" and reversed its meaning.

## 1.0.0

* Initial release
