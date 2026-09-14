Allows tweaking various death related values. You can make death softer or harder as desired by editing the config.

* Tune the death penalty skill level loss percentage.
* Toggle whether skill progress towards the next level is reset.
* Modify the duration of the "No Skill Loss" buff.
* Modify the duration of the "Corpse Run" buff.

Run the game once with the mod enabled to generate the config. See config for details on what each option does.

This mod must be installed on client and server.

Some configuration options can be enforced by a server running [ConditionalConfigSync](https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync). See included ConfigSync_Readme.txt file for more information.

## Installation

This mod uses [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim) as a mod loader. You can use any BepinEx compatible mod manager to install the mod, or manually place it in the BepixEx `plugins` directory.

## Valheim Skill Loss Calculation

Skills levels are stored as two values, the level itself and the percentage progress towards the next level.

When a player dies, the game calculate a skill loss factor using the formula `0.05 * SkillReductionRate`. `SkillReductionRate` is a global key determined by the death penalty world modifier. As of Valheim 1.0, these are the modifier values:
* Casual and very easy: `0.15`
* Easy: `0.5`
* Normal: `1.0`
* Hard: `1.5`

The game then loops through all of the player's skills and does the following:
1. Set the skill level to `current level - (current level * skill loss factor)`
2. Reset the progress towards the next level

Because the factor is multiplied by the current level, that means losses impact higher level skills considerably more than lower level skills. At standard settings, a level 20 skill would drop to 19 for a loss of about 42 experience while a level 100 skill would drop to 95 for a loss of about 2420 experience. (Experience per level follows the formula `next=Floor(current + 1)^1.5 * 0.5 + 0.5`.)

This mod allows adjusting the skill loss calculation in the following ways:
* Adjust the base `0.05` constant used in the loss factor calculation, represented by the config `SkillLossPercent` as a percentage value (like `5%`).
* Optionally skip resetting the progress towards the next level that normally occurs by setting the config `ResetLevelProgress` to `false`.
