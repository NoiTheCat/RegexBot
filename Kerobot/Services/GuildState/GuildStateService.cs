using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Discord.WebSocket;
using Kerobot.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kerobot.Services.GuildState
{
    /// <summary>
    /// Implements per-module storage and retrieval of guild-specific state data.
    /// This typically includes module configuration data.
    /// </summary>
    class GuildStateService : Service
    {
        private readonly object _storageLock = new object();
        private readonly Dictionary<ulong, EntityList> _moderators;
        private readonly Dictionary<ulong, Dictionary<Type, StateInfo>> _states;

        const string GuildLogSource = "Configuration loader";

        public GuildStateService(Kerobot kb) : base(kb)
        {
            _moderators = new Dictionary<ulong, EntityList>();
            _states = new Dictionary<ulong, Dictionary<Type, StateInfo>>();
            CreateDatabaseTablesAsync().Wait();
            
            kb.DiscordClient.GuildAvailable += DiscordClient_GuildAvailable;
            kb.DiscordClient.JoinedGuild += DiscordClient_JoinedGuild;
            kb.DiscordClient.LeftGuild += DiscordClient_LeftGuild;

            // TODO periodic task for refreshing stale configuration
        }

        private async Task DiscordClient_GuildAvailable(SocketGuild arg) => await InitializeGuild(arg);
        private async Task DiscordClient_JoinedGuild(SocketGuild arg) => await InitializeGuild(arg);

        /// <summary>
        /// Unloads in-memory guild information.
        /// </summary>
        private Task DiscordClient_LeftGuild(SocketGuild arg)
        {
            // TODO what is GuildUnavailable? Should we listen for that too?
            lock (_storageLock) _states.Remove(arg.Id);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Initializes guild in-memory structures and attempts to load configuration.
        /// </summary>
        private async Task InitializeGuild(SocketGuild arg)
        {
            // We're only loading config here now.
            bool success = await LoadGuildConfiguration(arg.Id);
            if (!success)
            {
                await Kerobot.GuildLogAsync(arg.Id, GuildLogSource,
                    "Configuration was not reloaded due to the previously stated error(s).");
            }
            else
            {
                await Kerobot.InstanceLogAsync(false, GuildLogSource,
                    $"Configuration successfully refreshed for guild ID {arg.Id}.");
            }
        }

        #region Data output
        /// <summary>
        /// See <see cref="ModuleBase.GetGuildState{T}(ulong)"/>.
        /// </summary>
        public T RetrieveGuildStateObject<T>(ulong guildId, Type t)
        {
            lock (_storageLock)
            {
                if (_states.TryGetValue(guildId, out var tl))
                {
                    if (tl.TryGetValue(t, out var val))
                    {
                        // Leave handling of potential InvalidCastException to caller.
                        return (T)val.Data;
                    }
                }
                return default;
            }
        }

        /// <summary>
        /// See <see cref="ModuleBase.GetModerators(ulong)"/>.
        /// </summary>
        public EntityList RetrieveGuildModerators(ulong guildId)
        {
            lock (_storageLock)
            {
                if (_moderators.TryGetValue(guildId, out var mods)) return mods;
                else return new EntityList();
            }
        }
        #endregion

        /// <summary>
        /// Guild-specific configuration begins processing here.
        /// Configuration is loaded from database, and appropriate sections dispatched to their
        /// respective methods for further processing.
        /// </summary>
        /// <remarks>
        /// This takes an all-or-nothing approach. Should there be a single issue in processing
        /// configuration, the old state data is kept.
        /// </remarks>
        private async Task<bool> LoadGuildConfiguration(ulong guildId)
        {
            
            var jstr = await RetrieveConfiguration(guildId);
            int jstrHash = jstr.GetHashCode();
            JObject guildConf;
            try
            {
                var tok = JToken.Parse(jstr);
                if (tok.Type == JTokenType.Object)
                {
                    guildConf = (JObject)tok;
                }
                else
                {
                    throw new InvalidCastException("The given configuration is not a JSON object.");
                }
            }
            catch (Exception ex) when (ex is JsonReaderException || ex is InvalidCastException)
            {
                await Kerobot.GuildLogAsync(guildId, GuildLogSource,
                    $"A problem exists within the guild configuration: {ex.Message}");

                // Don't update currently loaded state.
                return false;
            }

            // TODO Guild-specific service options? If implemented, this is where to load them.

            // Load moderator list
            var mods = new EntityList(guildConf["Moderators"], true);

            // Create guild state objects for all existing modules
            var newStates = new Dictionary<Type, StateInfo>();
            foreach (var mod in Kerobot.Modules)
            {
                var t = mod.GetType();
                var tn = t.Name;
                try
                {
                    try
                    {
                        var state = await mod.CreateGuildStateAsync(guildId, guildConf[tn]); // can be null
                        newStates.Add(t, new StateInfo(state, jstrHash));
                    }
                    catch (Exception ex) when (!(ex is ModuleLoadException))
                    {
                        Log("Unhandled exception while initializing guild state for module:\n" +
                            $"Module: {tn} | " +
                            $"Guild: {guildId} ({Kerobot.DiscordClient.GetGuild(guildId)?.Name ?? "unknown name"})\n" +
                            $"```\n{ex.ToString()}\n```", true).Wait();
                        Kerobot.GuildLogAsync(guildId, GuildLogSource,
                            "An internal error occurred when attempting to load new configuration. " +
                            "The bot owner has been notified.").Wait();
                        return false;
                    }
                }
                catch (ModuleLoadException ex)
                {
                    await Kerobot.GuildLogAsync(guildId, GuildLogSource,
                        $"{tn} has encountered an issue with its configuration: {ex.Message}");
                    return false;
                }
            }
            lock (_storageLock)
            {
                _moderators[guildId] = mods;
                _states[guildId] = newStates;
            }
            return true;
        }

        #region Database
        const string DBTableName = "guild_configuration";
        /// <summary>
        /// Creates the table structures for holding guild configuration.
        /// </summary>
        private async Task CreateDatabaseTablesAsync()
        {
            using (var db = await Kerobot.GetOpenNpgsqlConnectionAsync())
            {
                using (var c = db.CreateCommand())
                {
                    c.CommandText = $"create table if not exists {DBTableName} ("
                        + $"rev_id SERIAL primary key, "
                        + "guild_id bigint not null, "
                        + "author bigint not null, "
                        + "rev_date timestamptz not null default NOW(), "
                        + "config_json text not null"
                        + ")";
                    await c.ExecuteNonQueryAsync();
                }

                // Creating default configuration with revision ID 0.
                // Config ID 0 is used when no other configurations can be loaded for a guild.
                using (var c = db.CreateCommand())
                {
                    c.CommandText = $"insert into {DBTableName} (rev_id, guild_id, author, config_json) "
                        + "values (0, 0, 0, @Json) "
                        + "on conflict (rev_id) do nothing";
                    c.Parameters.Add("@Json", NpgsqlTypes.NpgsqlDbType.Text).Value = GetDefaultConfiguration();
                    c.Prepare();
                    await c.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task<string> RetrieveConfiguration(ulong guildId)
        {
            // Offline option: Per-guild configuration exists under `config/(guild ID).json`
            var basePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) +
                Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar;
            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
            var path = basePath + guildId + ".json";
            if (File.Exists(path))
            {
                return await File.ReadAllTextAsync(path);
            }
            else
            {
                await File.WriteAllTextAsync(path, GetDefaultConfiguration());
                await Log($"Created initial configuration file in config{Path.DirectorySeparatorChar}{guildId}.json");
                return await RetrieveConfiguration(guildId);
            }
        }
        #endregion

        /// <summary>
        /// Retrieves the default configuration loaded within the assembly.
        /// </summary>
        private string GetDefaultConfiguration()
        {
            const string ResourceName = "Kerobot.DefaultGuildConfig.json";

            var a = System.Reflection.Assembly.GetExecutingAssembly();
            using (var s = a.GetManifestResourceStream(ResourceName))
            using (var r = new System.IO.StreamReader(s))
                return r.ReadToEnd();
        }
    }
}
