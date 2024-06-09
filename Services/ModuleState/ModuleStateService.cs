using RegexBot.Common;

namespace RegexBot.Services.ModuleState;
/// <summary>
/// Implements per-module storage and retrieval of guild-specific state data, most typically but not limited to configuration data.
/// </summary>
class ModuleStateService : Service {
    private readonly Dictionary<ulong, EntityList> _moderators;
    private readonly Dictionary<ulong, Dictionary<Type, object?>> _guildStates;
    private readonly JObject _serverConfs;

    public ModuleStateService(RegexbotClient bot, JObject servers) : base(bot) {
        _moderators = [];
        _guildStates = [];
        _serverConfs = servers;

        bot.DiscordClient.GuildAvailable += RefreshGuildState;
        bot.DiscordClient.JoinedGuild += RefreshGuildState;
        bot.DiscordClient.LeftGuild += RemoveGuildData;
    }

    private async Task RefreshGuildState(SocketGuild arg) {
        if (await ProcessConfiguration(arg)) Log($"'{arg.Name}': Configuration refreshed.");
        else Log($"'{arg.Name}': Configuration refresh failed. Retaining existing configuration and state, if any.");
    }

    private Task RemoveGuildData(SocketGuild arg) {
        _guildStates.Remove(arg.Id);
        _moderators.Remove(arg.Id);
        return Task.CompletedTask;
    }

    // Hooked
    public T? DoGetStateObj<T>(ulong guildId, Type t) {
        if (_guildStates.TryGetValue(guildId, out var guildConfs)) {
            if (guildConfs.TryGetValue(t, out var moduleConf)) {
                // Leave handling of potential InvalidCastException to caller.
                return (T?)moduleConf;
            }
        }

        return default;
    }

    // Hooked
    public EntityList DoGetModlist(ulong guildId) {
        if (_moderators.TryGetValue(guildId, out var mods)) return mods;
        else return new EntityList();
    }

    private async Task<bool> ProcessConfiguration(SocketGuild guild) {
        var guildConf = _serverConfs[guild.Id.ToString()]?.Value<JObject>();
        if (guildConf == null) {
            Log($"{guild.Name} ({guild.Id}) has no configuration. Add config or consider removing bot from server.");
            return true;
        }

        // Load moderator list
        var mods = new EntityList(guildConf["Moderators"]!);

        // Create guild state objects for all existing modules
        var newStates = new Dictionary<Type, object?>();
        foreach (var module in BotClient.Modules) {
            var t = module.GetType();
            try {
                var state = await module.CreateGuildStateAsync(guild.Id, guildConf[module.Name]);
                newStates.Add(t, state);
            } catch (ModuleLoadException ex) {
                Log($"{module.Name} failed to read configuration for {guild.Name}: {ex.Message}");
                return false;
            } catch (Exception ex) {
                Log($"Unhandled exception from {module.Name} while creating guild state for {guild.Name}:\n{ex}");
                return false;
            }
        }
        _moderators[guild.Id] = mods;
        _guildStates[guild.Id] = newStates;
        return true;
    }
}
