A code library with utilities for mods to use. Developed primarily for use by mods developed by Crystal, but free for others to use if desired.

See the [Wiki](https://valheim.thunderstore.io/package/Crystal/CrystalLib/wiki) for details on what is included in the library.

## Installation
This library should install automatically when installing a mod that depends on it if you are using a mod manager such as [r2modman](https://thunderstore.io/package/ebkr/r2modman/). You can optionally install it manually following the steps below.

**Manual Install**

1. Install [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Download latest ``CrystalLib.dll`` by clicking "Manual Download". Extract the dll from the zip file into ``[GameDirectory]\Bepinex\plugins``. (You only need the dll.)

## Changelog

1.1.2

* Updated BepinEx version

1.1.1

* Updated for game compatibility

1.1.0

* **Breaking Change**: References to `UnityEngine.KeyCode` have been replaced by references to `UnityEngine.InputSystem.Key` which affects the API of the `InputBinding` class. This is necessary to follow changes in Valheim's input system.
* Updated .NET version

1.0.2

* Updated for game compatibility

1.0.1

* Updated for game compatibility

1.0.0

* Initial release
