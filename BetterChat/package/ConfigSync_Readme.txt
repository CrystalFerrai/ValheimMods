This mod uses ConditionalConfigSync to synchronize settings among players connected to a server.
For more information about ConditionalConfigSync and how to manage the configuration of a mod, see
https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync

By default, this mod is optional for all clients on a server. If you want to make it required, add
the following line to ConditionalConfigSync.ModRequirements.cfg in ConditionalConfigSync config.

+ dev.crystal.betterchat

This mod exposes the following properties.

# Section: dev.crystal.betterchat.Chat
dev.crystal.betterchat.Chat.AlwaysVisible
Policy: AlwaysClientControlled; Default: ClientControlled
dev.crystal.betterchat.Chat.DefaultShout
Policy: AlwaysClientControlled; Default: ClientControlled
dev.crystal.betterchat.Chat.ForceCase
Policy: AlwaysClientControlled; Default: ClientControlled
dev.crystal.betterchat.Chat.HideDelay
Policy: AlwaysClientControlled; Default: ClientControlled
dev.crystal.betterchat.Chat.ShowShoutPings
Policy: Conditional; Default: ServerControlled
dev.crystal.betterchat.Chat.SlashOpensChat
Policy: AlwaysClientControlled; Default: ClientControlled
dev.crystal.betterchat.Chat.TalkDistance
Policy: Conditional; Default: ServerControlled
dev.crystal.betterchat.Chat.WhisperDistance
Policy: Conditional; Default: ServerControlled
