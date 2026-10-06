Increases the base map discovery range around the player as well as dynamically adjusts the range based on various visibility factors.

* The discovery radius is generally increased compared to vanilla.
* Discovery radius is further increased while on a boat.
* Radius increases gradually based on altitude.
* Radius increases or decreases based on current amount of daylight.
* Radius can be decreased by weather effects such as fog, rain or snow.
* Radius decreased by 30% while in a forested area.

Values listed above are defaults. All of them are configurable. Run the game once with the mod enabled to generate the config. See config for details on what each option does.

It is generally recommended to only adjust the radius values in the "Base" category of the config. Default values in the "Multipliers" category have been tweaked to try to approximate actual visibility changes, and changing them can significantly impact exploration radius in sometimes unexpected ways.

**Important**: When tweaking config values in-game, it is possible to accidentally reveal a huge area of the map. So, it is recommended to tweak values either while in the main menu, when loaded into a map that you don't mind accidentally revealing, or when playing on a temporary character. Once an area is revealed, it will remain revealed for that character.

This is primarily a client side mod. It may also be installed on a server to sync configurations. A server may optionally require that clients have the mod installed via the `ConditionalConfigSync.ModRequirements.cfg` file in the ConditionalConfigSync config directory.

Some configuration options can be enforced by a server running [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync). See included ConfigSync_Readme.txt file for more information.

## Installation

This mod uses [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim) as a mod loader. You can use any BepinEx compatible mod manager to install the mod, or manually place it in the BepixEx `plugins` directory.
