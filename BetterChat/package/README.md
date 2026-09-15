Adds configurable features to the chat system.

* Configure how long the window shows when a new message is received.
* Optionally keep the chat window visible at all times.
* Remove the force caps from shouts and force lower case from whispers.
* Pressing the slash key (``/``) will open the chat window and start a message.
* Option to not see map pings when players shout.
* Switch so that the chat default is shout (use ``/say`` to not shout).
* Configure talk and whisper distances.
* Able to interact with UI that is behind the chat window (click-through).

Most everything listed is configurable. Run the game once with the mod enabled to generate the config. See config for details on what each option does.

This is primarily a client side mod. It may also be installed on a server to sync configurations. A server may optionally require that clients have the mod installed via the `ConditionalConfigSync.ModRequirements.cfg` file in the ConditionalConfigSync config directory.

Some configuration options can be enforced by a server running [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync). See included ConfigSync_Readme.txt file for more information.

## Installation

This mod uses [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim) as a mod loader. You can use any BepinEx compatible mod manager to install the mod, or manually place it in the BepixEx `plugins` directory.
