# Valheim Mod Collection

This repository hosts all of the Valheim mods created by Crystal. The primary intent of this repo is to serve as a reference for people who want to see how the mods work. If you are considering using code from this repo for your own projects, please read the license.txt file included at the root of the repo for redistribution details.

## Releases

Currently, mod releases are only being published to [Thunderstore](https://valheim.thunderstore.io/package/Crystal/). You can download releases from there, or you can install and use [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/) to download and manage the mods for you (recommended).

## How to Build

For easier development, all mods are part of the same repo and share the same Visual Studio solution. Each mod is a separate project within the solution.

Before the solution will build, you will need to get some third party DLLs and place them in a `lib` directory at the root of the repo (next to `ValheimMods.sln`). This is where all of the `.csproj` point to for external assembly references. The following should be included in the `lib` directory:

* [BepInEx](https://github.com/BepInEx/BepInEx/releases) library. Confirmed working version is 5.4.901, but other 5.4.x versions likely will also work. These files specifically need to be included:
    * 0Harmony.dll
    * BepinEx.dll
    * BepinEx.Harmony.dll
* Valheim script assemblies from the game client installation. These can be found in `valheim_Data/Managed` and start with `assembly_`. Currently in use are the following, but keep in mind this list may not be up to date. So others might also be needed.
    * assembly_guiutils.dll
    * assembly_valheim.dll
* Unstripped Unity engine assemblies that match the unity version used by Valheim. These can be found in different places, but I pulled them from [BepInExPack_Valheim on Thunderstore](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) (you may need to search if this link gets broken). This pack also includes the BepInEx assemblies listed above.

Once the `lib` directory is populated, you should be able to open `ValheimMods.sln` in Visual Studio 2019 and build the solution.

## How to Package for Release

The process for packaging up a build for release is not included in this repo. However, some of the supporting files are included. Within each project's root directory is a directory called `package` which contains these files.

The mod version needs to be updated in multiple places prior to building and packaging.

* Within the code itself, there is a `BepInPlugin` attribute on the main plugin class which includes the version number and needs to be updated.
* Also within the code, in `Properties/AssemblyInfo.cs`, both the `AssemblyVersion` and `AssemblyFileVersion` attributes should be updated.
* In the mod's `package` directory, open `manifest.json` and update both the `version_number` and `Version` properties.
* In the mod's `package` directory, open `README.md` and add a new version entry at the top of the "Changelog" section. Include notes about what changed since the last version.

Producing a releasable package involves combining the files in the `package` directory, along with a release build of the mod's dll, into a zip file named using the mod name and version, such as `BetterChat-1.0.0.zip`. Do not include the `package` directory itself in the zip file.