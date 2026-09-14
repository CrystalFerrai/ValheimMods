Health, stamina and eitr from food follows the curve ``y=1-x^8`` instead of the vanilla curve ``y=(1-x)^0.3``. This means that 50% of the way through the food, you are still getting nearly 100% of the benefit (vs 81%) and 75% of the way through you are still getting about 90% of the benefit (vs 65%). Values drop sharply as you near the end. This does not increase the overall duration of food, only makes more of the duration useful.

Run the game once with the mod enabled to generate the config. See config for details on what each option does.

Tip: The exponent of each curve is configurable. To visualize the curve and see how different exponents look, enter the above formula into a graphing calculator such as [this one](https://www.desmos.com/calculator) and change the ``8`` to whatever number you want to see.

This is primarily a client side mod. It may also be installed on a server to sync configurations. A server may optionally require that clients have the mod installed via the `ConditionalConfigSync.ModRequirements.cfg` file in the ConditionalConfigSync config directory.

Some configuration options can be enforced by a server running [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync). See included ConfigSync_Readme.txt file for more information.

## Installation

This mod uses [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim) as a mod loader. You can use any BepinEx compatible mod manager to install the mod, or manually place it in the BepixEx `plugins` directory.
