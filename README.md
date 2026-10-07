<div align="center">

# Meta Block

**Hide your server's plugin list from players in Counter-Strike 2.**

[![Release](https://img.shields.io/github/v/release/Ayrton09/MetaBlock?style=flat-square&color=success)](https://github.com/Ayrton09/MetaBlock/releases)
[![License](https://img.shields.io/github/license/Ayrton09/MetaBlock?style=flat-square)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square)](https://dotnet.microsoft.com/)
[![CounterStrikeSharp](https://img.shields.io/badge/CounterStrikeSharp-1.0.376-F5834A?style=flat-square)](https://docs.cssharp.dev/)

</div>

---

Any player can type `meta list`, `css_plugins list` or `css` in their console and see every plugin your server runs, with versions and authors. Meta Block stops that: players get a message of your choice instead, from the console and from chat. The server console and RCON keep working as always, and admins can be let through.

## Contents

- [Features](#features)
- [Installation](#installation)
- [Configuration](#configuration)
- [What Gets Blocked](#what-gets-blocked)
- [How It Works](#how-it-works)
- [Compatibility](#compatibility)

## Features

- Blocks Metamod's `meta` (`meta list`, `meta version`, `meta credits`), which no CounterStrikeSharp command listener can stop on its own
- Blocks CounterStrikeSharp's `css_plugins` and `css`, from the console and from chat (`!plugins`, `!css_plugins`, `/css`...)
- Any other console command can be added to the list
- Admins with a flag can still use the commands, or nobody can
- Configurable message, or a silent block
- The server console and RCON are never affected

## Installation

**1.** Download the latest ZIP from [Releases](https://github.com/Ayrton09/MetaBlock/releases).

**2.** Extract it into `game/csgo/`. The resulting layout:

```text
addons/counterstrikesharp/plugins/MetaBlock/
|-- MetaBlock.dll
`-- MetaBlock.deps.json
```

**3.** Start the server. The config is created on first start at:

```text
addons/counterstrikesharp/configs/plugins/MetaBlock/MetaBlock.json
```

Requires Metamod:Source 2.0 and CounterStrikeSharp 1.0.376 or newer.

## Configuration

```json
{
  "BlockedCommands": ["meta", "css_plugins", "css"],
  "AllowAdmins": true,
  "AdminFlag": "@css/root",
  "BlockMessage": "This command is disabled on this server.",
  "ConfigVersion": 1
}
```

| Option | Default | Description |
| --- | --- | --- |
| `BlockedCommands` | `meta`, `css_plugins`, `css` | Commands blocked for players, each one with all its subcommands. Names are not case sensitive |
| `AllowAdmins` | `true` | Let players with `AdminFlag` use the blocked commands. `false` blocks everyone, admins included |
| `AdminFlag` | `@css/root` | Flag that lets an admin through, as given in `configs/admins.json` |
| `BlockMessage` | `This command is disabled on this server.` | Shown in the player's console, or in chat for chat commands. `""` blocks silently |

The config is read when the plugin loads. After changing it, reload the plugin from the server console: `css_plugins list` shows its number, then `css_plugins reload <number>`. Restarting the server works too.

## What Gets Blocked

With the default config, a player without the admin flag gets the block message for:

| Where | Commands |
| --- | --- |
| Console | `meta`, `meta list`, `meta version`, `meta credits`, `css_plugins` with any option, `css` |
| Chat | `!css_plugins`, `!plugins`, `!css` and the silent `/` forms |

A blocked chat command is not shown to the other players either. Ordinary chat messages are never touched.

Chat commands are matched the way CounterStrikeSharp runs them, using the triggers from `core.json`: `!plugins` runs `css_plugins`, so blocking `css_plugins` also blocks it. `meta` is not a chat command, so `!meta` stays a normal message.

## How It Works

A command typed in a player's console reaches the server along one of two paths, and each blocked command is stopped before the code that answers it:

- **Commands the server does not register for players**, like Metamod's `meta`. They go to the game's client command handler, where Metamod answers `meta` before CounterStrikeSharp sees it, so a regular command listener only runs once the plugin list was already printed. Meta Block hooks that handler directly, ahead of Metamod.
- **Commands a player is allowed to run**, which includes every CounterStrikeSharp command. The engine dispatches them right away; Meta Block stops them in a global command listener, which CounterStrikeSharp runs before the command itself.
- **Chat commands** run from inside `say` and `say_team`, so Meta Block stops the chat message before CounterStrikeSharp reads it as a command.

## Compatibility

Tested on CS2 1.41.8.9 with Metamod:Source 2.0.0-dev+1469 and CounterStrikeSharp 1.0.376, on Windows and Linux.

The `meta` block hooks the client command handler by its position in the game's server interface (index 17, the same on Windows and Linux). A CS2 update that changes that interface would need a new Meta Block release, so check the releases page after a big game update if `meta` stops being blocked.

## License

Released under the [MIT License](LICENSE).
