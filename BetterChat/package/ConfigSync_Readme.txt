This mod uses ConditionalConfigSync to synchronize settings among players connected to a server.
For more information about ConditionalConfigSync and how to manage the configuration of a mod, see
https://thunderstore.io/c/valheim/p/shudnal/ConditionalConfigSync

This mod exposes the following properties.

# Section: dev.crystal.betterchat.ServerSync
dev.crystal.betterchat.ServerSync.ModRequired
Policy: AlwaysServerControlled; Default: ServerControlled

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
