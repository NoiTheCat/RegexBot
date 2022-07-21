using Newtonsoft.Json;
using RegexBot.Common;
using System.Reflection;

namespace RegexBot.Services.ModuleState;
/// <summary>
/// Implements per-module storage and retrieval of guild-specific state data, most typically but not limited to configuration data.
/// To that end, this service handles loading and validation of per-guild configuration files.
/// </summary>
class ModuleStateService : Service {
    private readonly object _storageLock = new();
    private readonly Dictionary<ulong, EntityList> _moderators;
    private readonly Dictionary<ulong, Dictionary<Type, object?>> _stateData;

    public ModuleStateService(RegexbotClient bot) : base(bot) {
        _moderators = new();
        _stateData = new();

        bot.DiscordClient.GuildAvailable += RefreshGuildState;
        bot.DiscordClient.JoinedGuild += RefreshGuildState;
        bot.DiscordClient.LeftGuild += RemoveGuildData;
    }

    private async Task RefreshGuildState(SocketGuild arg) {
        if (await ProcessConfiguration(arg.Id)) Log($"Configuration refreshed for server {arg.Id}.");
    }

    private Task RemoveGuildData(SocketGuild arg) {
        lock (_storageLock) {
            _stateData.Remove(arg.Id);
            _moderators.Remove(arg.Id);
        }
        return Task.CompletedTask;
    }

    // Hooked
    public T? DoGetStateObj<T>(ulong guildId, Type t) {
        lock (_storageLock) {
            if (_stateData.ContainsKey(guildId) && _stateData[guildId].ContainsKey(t)) {
                // Leave handling of potential InvalidCastException to caller.
                return (T?)_stateData[guildId][t];
            }
            return default;
        }
    }

    // Hooked
    public EntityList DoGetModlist(ulong guildId) {
        lock (_storageLock) {
            if (_moderators.TryGetValue(guildId, out var mods)) return mods;
            else return new EntityList();
        }
    }

    /// <summary>
    /// Configuration is loaded from database, and appropriate sections dispatched to their
    /// respective methods for further processing.
    /// </summary>
    /// <remarks>
    /// This takes an all-or-nothing approach. Should there be a single issue in processing
    /// configuration, all existing state data is kept.
    /// </remarks>
    private async Task<bool> ProcessConfiguration(ulong guildId) {
        var jstr = await LoadConfigFile(guildId);
        JObject guildConf;
        try {
            var tok = JToken.Parse(jstr);
            if (tok.Type == JTokenType.Object) {
                guildConf = (JObject)tok;
            } else {
                throw new InvalidCastException("Configuration is not valid JSON.");
            }
        } catch (Exception ex) when (ex is JsonReaderException or InvalidCastException) {
            Log($"Error loading configuration for server ID {guildId}: {ex.Message}");
            return false;
        }

        // TODO Guild-specific service options? If implemented, this is where to load them.

        // Load moderator list
        var mods = new EntityList(guildConf["Moderators"]!, true);

        // Create guild state objects for all existing modules
        var newStates = new Dictionary<Type, object?>();
        foreach (var module in BotClient.Modules) {
            var t = module.GetType();
            try {
                var state = await module.CreateGuildStateAsync(guildId, guildConf[module.Name]!);
                newStates.Add(t, state);
            } catch (ModuleLoadException ex) {
                Log($"{guildId}: Error reading configuration regarding {module.Name}: {ex.Message}");
                return false;
            } catch (Exception ex) when (ex is not ModuleLoadException) {
                Log("Unhandled exception while initializing guild state for module:\n" +
                    $"Module: {module.Name} | " +
                    $"Guild: {guildId} ({BotClient.DiscordClient.GetGuild(guildId)?.Name ?? "unknown name"})\n" +
                    $"```\n{ex}\n```", true);
                return false;
            }
        }
        lock (_storageLock) {
            _moderators[guildId] = mods;
            _stateData[guildId] = newStates;
        }
        return true;
    }

    private async Task<string> LoadConfigFile(ulong guildId) {
        // Per-guild configuration exists under `config/(guild ID).json`
        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly()!.Location) +
            Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar;
        if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
        var path = basePath + guildId + ".json";
        if (File.Exists(path)) {
            return await File.ReadAllTextAsync(path);
        } else {
            // Write default configuration to new file
            using var resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(RegexBot)}.DefaultGuildConfig.json");
            using (var newFile = File.OpenWrite(path)) resStream!.CopyTo(newFile);
            Log($"Created initial configuration file in config{Path.DirectorySeparatorChar}{guildId}.json");
            return await LoadConfigFile(guildId);
        }
    }
}
