This mod uses ConditionalConfigSync to synchronize settings among players connected to a server.
For more information about ConditionalConfigSync and how to manage the configuration of a mod, see
https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync

By default, this mod is required for all clients on a server. If you want to make it optional, add
the following line to ConditionalConfigSync.ModRequirements.cfg in ConditionalConfigSync config.

- dev.crystal.magical

NOTE: Allowing clients to connect without the mod may result in some mod features not behaving
properly in some scenarios.

This mod exposes the following properties.

# Section: dev.crystal.magical.Base
dev.crystal.magical.Base.BaseEitr
Policy: AlwaysServerControlled; Default: ServerControlled
dev.crystal.magical.Base.BaseHealth
Policy: AlwaysServerControlled; Default: ServerControlled
dev.crystal.magical.Base.BaseStamina
Policy: AlwaysServerControlled; Default: ServerControlled

# Section: dev.crystal.magical.Regen
dev.crystal.magical.Regen.BaseEitrRegen
Policy: AlwaysServerControlled; Default: ServerControlled
dev.crystal.magical.Regen.BaseHealthRegen
Policy: AlwaysServerControlled; Default: ServerControlled
dev.crystal.magical.Regen.BaseStaminaRegen
Policy: AlwaysServerControlled; Default: ServerControlled
dev.crystal.magical.Regen.EitrRegenDelay
Policy: AlwaysServerControlled; Default: ServerControlled
dev.crystal.magical.Regen.HealthRegenTickRate
Policy: AlwaysServerControlled; Default: ServerControlled
dev.crystal.magical.Regen.StaminaRegenDelay
Policy: AlwaysServerControlled; Default: ServerControlled

# Section: dev.crystal.magical.Skill
dev.crystal.magical.Skill.SkillEitrReduction
Policy: AlwaysServerControlled; Default: ServerControlled
dev.crystal.magical.Skill.SkillHealthReduction
Policy: AlwaysServerControlled; Default: ServerControlled
dev.crystal.magical.Skill.SkillStaminaReduction
Policy: AlwaysServerControlled; Default: ServerControlled
