This mod uses ConditionalConfigSync to synchronize settings among players connected to a server.
For more information about ConditionalConfigSync and how to manage the configuration of a mod, see
https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync

By default, this mod is required for all clients on a server. If you want to make it optional, add
the following line to ConditionalConfigSync.ModRequirements.cfg in ConditionalConfigSync config.

- dev.crystal.gracefullanding

NOTE: Allowing clients to connect without the mod may result in some mod features not behaving
properly in some scenarios.

This mod exposes the following properties.

# Section: dev.crystal.gracefullanding.Falling
dev.crystal.gracefullanding.Falling.MaxDamageAmount
Policy: AlwaysServerControlled; Default: ServerControlled
dev.crystal.gracefullanding.Falling.MaxDamageHeight
Policy: AlwaysServerControlled; Default: ServerControlled
dev.crystal.gracefullanding.Falling.MinDamageHeight
Policy: AlwaysServerControlled; Default: ServerControlled
