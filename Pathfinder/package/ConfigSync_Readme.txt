This mod uses ConditionalConfigSync to synchronize settings among players connected to a server.
For more information about ConditionalConfigSync and how to manage the configuration of a mod, see
https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync

By default, this mod is required for all clients on a server. If you want to make it optional, add
the following line to ConditionalConfigSync.ModRequirements.cfg in ConditionalConfigSync config.

- dev.crystal.pathfinder

NOTE: Allowing clients to connect without the mod may result in some mod features not behaving
properly in some scenarios.

This mod exposes the following properties.

# Section: dev.crystal.pathfinder.Base
dev.crystal.pathfinder.Base.LandExploreRadius
Policy: AlwaysServerControlled; Default: ServerControlled
dev.crystal.pathfinder.Base.MaximumRadius
Policy: AlwaysServerControlled; Default: ServerControlled
dev.crystal.pathfinder.Base.MinimumRadius
Policy: AlwaysServerControlled; Default: ServerControlled
dev.crystal.pathfinder.Base.SeaExploreRadius
Policy: AlwaysServerControlled; Default: ServerControlled

# Section: dev.crystal.pathfinder.Miscellaneous
dev.crystal.pathfinder.Miscellaneous.DisplayCurrentRadiusValue
Policy: AlwaysClientControlled; Default: ClientControlled
dev.crystal.pathfinder.Miscellaneous.DisplayVariables
Policy: AlwaysClientControlled; Default: ClientControlled

# Section: dev.crystal.pathfinder.Multipliers
dev.crystal.pathfinder.Multipliers.AltitudeRadiusBonus
Policy: AlwaysServerControlled; Default: ServerControlled
dev.crystal.pathfinder.Multipliers.DaylightRadiusScale
Policy: AlwaysServerControlled; Default: ServerControlled
dev.crystal.pathfinder.Multipliers.ForestRadiusPenalty
Policy: AlwaysServerControlled; Default: ServerControlled
dev.crystal.pathfinder.Multipliers.WeatherRadiusScale
Policy: AlwaysServerControlled; Default: ServerControlled
