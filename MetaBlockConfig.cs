using CounterStrikeSharp.API.Core;

namespace MetaBlock;

public sealed class MetaBlockConfig : BasePluginConfig
{
    // Command names blocked when a player sends them. The server console and RCON are never blocked.
    public List<string> BlockedCommands { get; set; } = ["meta", "css_plugins", "css"];

    // When true, players holding AdminFlag can still use the blocked commands.
    // Set it to false to block everyone, admins included.
    public bool AllowAdmins { get; set; } = true;

    public string AdminFlag { get; set; } = "@css/root";

    // Printed to the player's console (or chat, for chat commands). Leave it empty to block silently.
    public string BlockMessage { get; set; } = "This command is disabled on this server.";
}
