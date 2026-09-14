## 1.6.3

* New config option `ShowOnNewMessage` controls whether the mod will force the chat window to appear when a new message is received. Set to `true` by default which is consistent with the previous mod behavior.
* Updated mod package and documentation.

## 1.6.2

* Update to ConditionalConfigSync 1.0.6 which supports server-controlled configuration for requiring the mod on clients.
* Removed `ModRequired` config value. The new ConditionalConfigSync.ModRequirements.cfg file in the ConditionalConfigSync config replaces this feature.

## 1.6.1

* Added `ModRequired` config value. If set to true on a server, connecting clients must have the mod installed. If false (default), the mod is optional for clients.

## 1.6.0

* Added server config sync via [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync)

## 1.5.0

* Updated for Valheim 1.0

## 1.4.11

* Updated for game compatibility
* Updated BepinEx version

## 1.4.10

* Updated for game compatibility

## 1.4.9

* Updated BepinEx version
* Updated .NET version

## 1.4.8

* Updated for game compatibility

## 1.4.7

* Updated BepinEx version

## 1.4.6

* Updated for game compatibility

## 1.4.5

* Updated BepinEx version

## 1.4.4

* Removed erroneously added shout distance setting which didn't actually do anything

## 1.4.3

* Updated for game compatibility
* Updated BepInEx version

## 1.4.2

* Updated for compatibility with game update
* Updated BepInEx version

## 1.4.1

* Changing the mod config live (via something like BepInEx Configuration Manager) is now supported.

## 1.4.0

* Pressing the slash key (/) will now open chat and start typing (can be disabled).
* New config option to not see map pings when people shout.

## 1.3.0

* New option to make chat default to shout.
* New options to configure talk and whisper listen distances.
* Numeric options now have range limits.

## 1.2.0

* Shouts are no longer in all caps nor whispers all lower case. This can be changed in config.

## 1.1.0

* By default, the chat window will now show whenever a new message is received (instead of always). This can be changed in the config.
* Chat window is now click-through.

## 1.0.1

* Now incldues proper dll. Somehow 1.0.0 had a work-in-progress version included in it.

## 1.0.0

* Initial release
