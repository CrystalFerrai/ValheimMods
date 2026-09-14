Increases the base map discovery range around the player as well as dynamically adjusts the range based on various visibility factors.

* The discovery radius is generally increased compared to vanilla.
* Discovery radius is further increased while on a boat.
* Radius increases gradually based on altitude.
* Radius increases or decreases based on current amount of daylight.
* Radius can be decreased by weather effects such as fog, rain or snow.
* Radius decreased by 30% while in a forested area.

Values listed above are defaults. All of them are configurable. Run the game once with the mod enabled to generate the config. See config for details on what each option does.

It is generally recommended to only adjust the radius values in the "Base" category of the config. Default values in the "Multipliers" category have been tweaked to try to approximate actual visibility changes, and changing them can significantly impact exploration radius in sometimes unexpected ways.

This mod should be installed on client and server. Mod features may not work reliably if any clients do not have the mod installed. By default, a server running the mod will reject clients that do not have it installed. To allow unmodded clients, at the risk of unreliable functionality, a server may disable the client mod requirement via the `ConditionalConfigSync.ModRequirements.cfg` file in the ConditionalConfigSync config directory.

Some configuration options can be enforced by a server running [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync). See included ConfigSync_Readme.txt file for more information.

## Installation

This mod uses [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim) as a mod loader. You can use any BepinEx compatible mod manager to install the mod, or manually place it in the BepixEx `plugins` directory.
