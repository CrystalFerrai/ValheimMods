This mod uses ConditionalConfigSync to synchronize settings among players connected to a server.
For more information about ConditionalConfigSync and how to manage the configuration of a mod, see
https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync

By default, this mod is optional for all clients on a server. If you want to make it required, add
the following line to ConditionalConfigSync.ModRequirements.cfg in ConditionalConfigSync config.

+ dev.crystal.underwater

This mod exposes the following properties.

# Section: dev.crystal.underwater.Underwater
dev.crystal.underwater.Underwater.CameraIgnoreWater
Policy: AlwaysClientControlled; Default: ClientControlled
dev.crystal.underwater.Underwater.ModUseAllowed
Policy: AlwaysServerControlled; Default: ServerControlled
dev.crystal.underwater.Underwater.PlayerSwims
Policy: AlwaysClientControlled; Default: ClientControlled
dev.crystal.underwater.Underwater.ToggleSwimKey
Policy: AlwaysClientControlled; Default: ClientControlled
