Health and stamina from food follows the curve ``y=1-x^8`` instead of the vanilla curve ``y=(1-x)^0.3``. This means that 50% of the way through the food, you are still getting nearly 100% of the benefit (vs 81%) and 75% of the way through you are still getting about 90% of the benefit (vs 65%). Values drop sharply as you near the end. This does not increase the overall duration of food, only makes more of the duration useful.

Run the game once with the mod enabled to generate the config. See config for details on what each option does.

Tip: The exponent of the curve is configurable. To visualize the food curve and see how different exponents look, enter the above formula into a graphing calculator such as [this one](https://www.desmos.com/calculator) and change the ``8`` to whatever number you want to see.

## Installation
This mod is designed to install and run via [r2modman](https://thunderstore.io/package/ebkr/r2modman/). You can optionally install it manually following the steps below.

**Manual Install**
1. Install [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Download latest ``Sated.dll`` by clicking "Manual Download". Extract the dll from the zip file into ``[GameDirectory]\Bepinex\plugins``. (You only need the dll.)
3. Run the game once, then close it and edit the generated cfg file in ``[GameDirectory]\Bepinex\config`` if you want to customize anything.

## Changelog
1.1.8

* Modified patching method for compatibility with Azumatt's MagicEitrBase mod.

1.1.7

* Fixed an issue preventing food from providing Eitr.

1.1.6

* Updated BepInEx version

1.1.5

* Fixed the time input to the curve function resulting in a proper drop off near the end. (This stopped working at some point due to a change to the vanilla formula.)

1.1.4

* Fixed some errors in total health and stamina calculations.

1.1.3

* Removed the food timer bar feature because it breaks the UI after the latest game update.
* Updated BepInEx version

1.1.2

* Changing the mod config live (via something like BepInEx Configuration Manager) is now supported.

1.1.1

* Added config options to adjust the health and stamina food curve exponents.

1.1.0

* Food icons now have timer bars below them (can be disabled).

1.0.2

* The food bar on the HUD now properly matches the HP bar.

1.0.1

* Bad upload. This version contained the 1.0.0 dll.

1.0.0

* Initial release
