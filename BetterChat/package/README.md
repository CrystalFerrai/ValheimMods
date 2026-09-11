Adds configurable features to the chat system.

* Show the chat window when a message is received, or have it always visible.
* Configure how long the window shows when a new message is received.
* Remove the force caps from shouts and force lower case from whispers.
* Pressing the slash key (``/``) will open the chat window and start a message.
* Option to not see map pings when players shout.
* Switch so that the chat default is shout (use ``/say`` to not shout).
* Configure talk and whisper distances.
* Able to interact with UI that is behind the chat window (click-through).

Most everything listed is configurable. Run the game once with the mod enabled to generate the config. See config for details on what each option does.

This is primarily a client side mod. It may also be installed on a server to sync configurations. A server may optionally require that clients have the mod installed by setting `ModRequired`=`true` in the config.

Some configuration options can be enforced by a server running [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync). See included ConfigSync_Readme.txt file for more information.

## Installation
This mod is designed to install and run via a mod manager such as [r2modman](https://thunderstore.io/package/ebkr/r2modman/). You can optionally install it manually following the steps below.

**Manual Install**

1. Install [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Install [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync)
3. Download latest ``BetterChat.dll`` by clicking "Manual Download". Extract the dll from the zip file into ``[GameDirectory]\Bepinex\plugins``. (You only need the dll.)
4. Run the game once, then close it and edit the generated cfg file in ``[GameDirectory]\Bepinex\config`` if you want to customize anything (or use a configuration management mod).

## Changelog

1.6.1

* Added `ModRequired` config value. If set to true on a server, connecting clients must have the mod installed. If false (default), the mod is optional for clients.

1.6.0

* Added server config sync via [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync)

1.5.0

* Updated for Valheim 1.0

1.4.11

* Updated for game compatibility
* Updated BepinEx version

1.4.10

* Updated for game compatibility

1.4.9

* Updated BepinEx version
* Updated .NET version

1.4.8

* Updated for game compatibility

1.4.7

* Updated BepinEx version

1.4.6

* Updated for game compatibility

1.4.5

* Updated BepinEx version

1.4.4

* Removed erroneously added shout distance setting which didn't actually do anything

1.4.3

* Updated for game compatibility
* Updated BepInEx version

1.4.2

* Updated for compatibility with game update
* Updated BepInEx version

1.4.1

* Changing the mod config live (via something like BepInEx Configuration Manager) is now supported.

1.4.0

* Pressing the slash key (/) will now open chat and start typing (can be disabled).
* New config option to not see map pings when people shout.

1.3.0

* New option to make chat default to shout.
* New options to configure talk and whisper listen distances.
* Numeric options now have range limits.

1.2.0

* Shouts are no longer in all caps nor whispers all lower case. This can be changed in config.

1.1.0

* By default, the chat window will now show whenever a new message is received (instead of always). This can be changed in the config.
* Chat window is now click-through.

1.0.1

* Now incldues proper dll. Somehow 1.0.0 had a work-in-progress version included in it.

1.0.0

* Initial release
