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

## Installation
This mod is designed to install and run via [r2modman](https://thunderstore.io/package/ebkr/r2modman/). You can optionally install it manually following the steps below.

**Manual Install**

1. Install [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Download latest ``BetterChat.dll`` by clicking "Manual Download". Extract the dll from the zip file into ``[GameDirectory]\Bepinex\plugins``. (You only need the dll.)
3. Run the game once, then close it and edit the generated cfg file in ``[GameDirectory]\Bepinex\config`` if you want to customize anything.

## Changelog
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
