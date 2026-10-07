using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using Microsoft.Extensions.Logging;

namespace MetaBlock;

[MinimumApiVersion(376)]
public sealed class MetaBlockPlugin : BasePlugin, IPluginConfig<MetaBlockConfig>
{
    private const string Version = "1.0.0";

    // ISource2GameClients::ClientCommand: 11 virtuals inherited from IAppSystem plus 6 declared
    // before it. Same slot on Windows and Linux because neither class has a virtual destructor.
    private const int ClientCommandVtableIndex = 17;

    private VirtualFunctionVoid<nint, int, nint>? _clientCommand;
    private HashSet<string> _blocked = new(StringComparer.OrdinalIgnoreCase);

    public override string ModuleName => "Meta Block";
    public override string ModuleVersion => Version;
    public override string ModuleAuthor => "Ayrton09";
    public override string ModuleDescription => "Blocks meta, css_plugins and css for players.";

    public MetaBlockConfig Config { get; set; } = new();

    public void OnConfigParsed(MetaBlockConfig config)
    {
        Config = config;
        _blocked = new HashSet<string>(
            config.BlockedCommands.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()),
            StringComparer.OrdinalIgnoreCase);
    }

    public override void Load(bool hotReload)
    {
        // Metamod answers a client's "meta" in its own pre-hook on ClientCommand, which runs
        // before any CounterStrikeSharp command listener, so a listener alone is too late.
        // Detouring the virtual directly puts this check in front of Metamod.
        _clientCommand = new VirtualFunctionVoid<nint, int, nint>(ValveInterface.ServerGameClients.Pointer, ClientCommandVtableIndex);
        _clientCommand.Hook(OnClientCommand, HookMode.Pre);

        // Commands a client is allowed to execute, which includes every CounterStrikeSharp
        // command, skip ClientCommand: the engine dispatches them straight away, so they are
        // stopped in a listener instead. It has to be the global listener (null name), which
        // runs before the per-command callbacks; a listener on "css_plugins" itself runs after
        // the handler CounterStrikeSharp registered first, when the list is already printed.
        AddCommandListener(null, (player, info) => OnBlockedCommand(player, info), HookMode.Pre);

        // Chat commands never reach ClientCommand under their own name: CounterStrikeSharp runs
        // them from inside "say", so stopping the say itself is what keeps them from executing.
        // Separate lambdas because listeners are stored per delegate.
        AddCommandListener("say", (player, info) => OnSay(player, info), HookMode.Pre);
        AddCommandListener("say_team", (player, info) => OnSay(player, info), HookMode.Pre);
    }

    public override void Unload(bool hotReload)
    {
        _clientCommand?.Unhook(OnClientCommand, HookMode.Pre);
        _clientCommand = null;
    }

    private HookResult OnClientCommand(DynamicHook hook)
    {
        try
        {
            var command = hook.GetParam<nint>(2);
            if (command == 0 || NativeAPI.CommandGetArgCount(command) < 1)
                return HookResult.Continue;

            var name = NativeAPI.CommandGetArgByIndex(command, 0);
            if (!_blocked.Contains(name))
                return HookResult.Continue;

            var player = Utilities.GetPlayerFromSlot(hook.GetParam<int>(1));
            if (IsExempt(player))
                return HookResult.Continue;

            if (player != null && !string.IsNullOrEmpty(Config.BlockMessage))
                player.PrintToConsole(Config.BlockMessage);
            return HookResult.Stop;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to check a client command.");
            return HookResult.Continue;
        }
    }

    private HookResult OnBlockedCommand(CCSPlayerController? player, CommandInfo info)
    {
        // Chat runs these through "say", which OnSay already handles, and the server console
        // and RCON come in without a player.
        if (player == null || info.CallingContext != CommandCallingContext.Console || info.ArgCount < 1)
            return HookResult.Continue;

        if (!_blocked.Contains(info.GetArg(0)) || IsExempt(player))
            return HookResult.Continue;

        if (!string.IsNullOrEmpty(Config.BlockMessage))
            player.PrintToConsole(Config.BlockMessage);
        return HookResult.Stop;
    }

    private HookResult OnSay(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || info.ArgCount < 2 || IsExempt(player))
            return HookResult.Continue;

        if (!IsBlockedChatCommand(info.GetArg(1)))
            return HookResult.Continue;

        if (!string.IsNullOrEmpty(Config.BlockMessage))
            player.PrintToChat(Config.BlockMessage);
        return HookResult.Stop;
    }

    // Mirrors how CounterStrikeSharp turns chat into a command: a configured trigger, then a
    // name that is tried with the "css_" prefix first, so "!plugins" and "!css_plugins" both
    // run css_plugins. Only CounterStrikeSharp commands can be run from chat.
    private bool IsBlockedChatCommand(string message)
    {
        foreach (var trigger in CoreConfig.PublicChatTrigger.Concat(CoreConfig.SilentChatTrigger))
        {
            if (string.IsNullOrEmpty(trigger) || !message.StartsWith(trigger, StringComparison.Ordinal))
                continue;

            var name = message[trigger.Length..].TrimStart().Split(' ', 2)[0];
            if (name.Length == 0)
                continue;

            if (_blocked.Contains("css_" + name) || (name.StartsWith("css", StringComparison.OrdinalIgnoreCase) && _blocked.Contains(name)))
                return true;
        }
        return false;
    }

    private bool IsExempt(CCSPlayerController? player)
    {
        if (!Config.AllowAdmins || string.IsNullOrWhiteSpace(Config.AdminFlag))
            return false;
        return player is { IsValid: true } && AdminManager.PlayerHasPermissions(player, Config.AdminFlag);
    }
}
