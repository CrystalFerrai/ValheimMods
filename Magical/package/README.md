Allows customization of many values related to player stats via the mod configuration.
* Base Health, Stamina and Eitr.
* Health, stamina and eitr regeneration rates.
* Reduction to helath, stamina and eitr cost of actions based on player skills.

The primary goal of the mod is to allow rebalancing the resource usage of magical and physical attacks. All configuration values default to vanilla game values.

Run the game once with the mod enabled to generate the config. See config for details on what each option does.

This mod should be installed on client and server. Mod features may not work reliably if any clients do not have the mod installed. By default, a server running the mod will reject clients that do not have it installed. To allow unmodded clients, at the risk of unreliable functionality, a server may disable the client mod requirement via the `ConditionalConfigSync.ModRequirements.cfg` file in the ConditionalConfigSync config directory.

Some configuration options can be enforced by a server running [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync). See included ConfigSync_Readme.txt file for more information.

## Installation

This mod uses [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim) as a mod loader. You can use any BepinEx compatible mod manager to install the mod, or manually place it in the BepixEx `plugins` directory.
