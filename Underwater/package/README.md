Adds an option for players to walk along the seafloor instead of swimming at the water surface. This option can be changed in the mod config or by pressing the `Backspace` key in game (key can be changed in config).

This mod was created primarily to assist with placing building pieces underwater. I may try to add diving controls in the future, but for now the options are to swim on the surface or walk on the bottom.

Run the game once with the mod enabled to generate the config. See config for details on what each option does.

This mod is client only and does not need to be installed on dedicated servers.

## Installation
This mod is designed to install and run via [r2modman](https://thunderstore.io/package/ebkr/r2modman/). You can optionally install it manually following the steps below.

**Manual Install**

1. Install [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Download latest ``Underwater.dll`` by clicking "Manual Download". Extract the dll from the zip file into ``[GameDirectory]\Bepinex\plugins``. (You only need the dll.)
3. Run the game once, then close it and edit the generated cfg file in ``[GameDirectory]\Bepinex\config`` if you want to customize anything.

## Changelog
1.0.5

* Updated CrystalLib dependency version

1.0.4

* Added dependency on CrystalLib and moved some code there.

1.0.3

* Fixed CameraIgnoreWater config option not applying properly.
* Fixed swim toggle keybind not working in multiplayer if there were an even number of players present.

1.0.2

* Updated BepinEx version

1.0.1

* The shortcut key for toggling player swimming can now be changed in the mod config.
* Renamed the setting "PlayerIgnoreWater" to "PlayerSwims" and reversed its meaning.

1.0.0

* Initial release
