using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kerobot.Services.GuildState
{
    /// <summary>
    /// Implements per-module storage and retrieval of guild-specific state data.
    /// This typically includes module configuration data.
    /// </summary>
    class GuildStateService : Service
    {
        // SCHEMAS ARE CREATED HERE, SOMEWHERE BELOW. MAKE SURE THIS CONSTRUCTOR IS CALLED EARLY.

        private readonly object _storageLock = new object();
        private readonly Dictionary<ulong, Dictionary<Type, StateInfo>> _storage;

        const string GuildLogSource = "Configuration loader";

        public GuildStateService(Kerobot kb) : base(kb)
        {
            _storage = new Dictionary<ulong, Dictionary<Type, StateInfo>>();

            _defaultGuildJson = PreloadDefaultGuildJson();
            
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
            lock (_storageLock) _storage.Remove(arg.Id);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Initializes guild in-memory and database structures, then attempts to load configuration.
        /// </summary>
        private async Task InitializeGuild(SocketGuild arg)
        {
            // Get this done before any other thing.
            await CreateSchema(arg.Id);

            // Attempt initialization on the guild. All services will set up their tables here.
            using (var db = await Kerobot.GetOpenNpgsqlConnectionAsync(arg.Id))
            {
                foreach (var svc in Kerobot.Services)
                {
                    try
                    {
                        await svc.CreateDatabaseTablesAsync(db);
                    }
                    catch (NpgsqlException ex)
                    {
                        await Log("Database error on CreateDatabaseTablesAsync:\n"
                            + $"-- Service: {svc.Name}\n-- Guild: {arg.Id}\n-- Error: {ex.Message}", true);
                    }
                }
            }

            // Then start loading guild information
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

        /// <summary>
        /// See <see cref="ModuleBase.GetGuildState{T}(ulong)"/>.
        /// </summary>
        public T RetrieveGuildStateObject<T>(ulong guildId, Type t)
        {
            lock (_storageLock)
            {
                if (_storage.TryGetValue(guildId, out var tl))
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

            var newStates = new Dictionary<Type, StateInfo>();
            foreach (var mod in Kerobot.Modules)
            {
                var t = mod.GetType();
                var tn = t.Name;
                try
                {
                    object state;
                    try
                    {
                        state = await mod.CreateGuildStateAsync(guildConf[tn]); // can be null
                    }
                    catch (Exception ex)
                    {
                        Log("Encountered unhandled exception during guild state initialization:\n" +
                            $"Module: {tn}\n" +
                            $"Guild: {guildId} ({Kerobot.DiscordClient.GetGuild(guildId)?.Name ?? "unknown name"})\n" +
                            $"```\n{ex.ToString()}\n```", true).Wait();
                        Kerobot.GuildLogAsync(guildId, GuildLogSource,
                            "An internal error occurred when attempting to load new configuration. " +
                            "The bot owner has been notified.").Wait();
                        return false;
                    }
                    newStates.Add(t, new StateInfo(state, jstrHash));
                }
                catch (ModuleLoadException ex)
                {
                    await Kerobot.GuildLogAsync(guildId, GuildLogSource,
                        $"{tn} has encountered an issue with its configuration: {ex.Message}");
                    return false;
                }
            }
            lock (_storageLock) _storage[guildId] = newStates;
            return true;
        }

        #region Database
        /// <summary>
        /// Creates a schema for holding all guild data.
        /// Ensure that this runs first before any other database call to a guild.
        /// </summary>
        private async Task CreateSchema(ulong guildId)
        {
            using (var db = await Kerobot.GetOpenNpgsqlConnectionAsync(null))
            {
                using (var c = db.CreateCommand())
                {
                    c.CommandText = $"create schema if not exists guild_{guildId}";
                    await c.ExecuteNonQueryAsync();
                }
            }
        }

        const string DBTableName = "guild_configuration";
        /// <summary>
        /// Creates the table structures for holding module configuration.
        /// </summary>
        public override async Task CreateDatabaseTablesAsync(NpgsqlConnection db)
        {
            using (var c = db.CreateCommand())
            {
                c.CommandText = $"create table if not exists {DBTableName} ("
                    + $"rev_id SERIAL primary key, "
                    + "author bigint not null, "
                    + "rev_date timestamptz not null default NOW(), "
                    + "config_json text not null"
                    + ")";
                await c.ExecuteNonQueryAsync();
            }
            // Creating default configuration with revision ID 0.
            // This allows us to quickly define rev_id as type SERIAL and not have to configure it so that
            // the serial should start at 2, but rather can easily start at 1. So lazy.
            using (var c = db.CreateCommand())
            {
                c.CommandText = $"insert into {DBTableName} (rev_id, author, config_json)"
                    + "values (0, 0, @Json) "
                    + "on conflict (rev_id) do nothing";
                c.Parameters.Add("@Json", NpgsqlTypes.NpgsqlDbType.Text).Value = _defaultGuildJson;
                c.Prepare();
                await c.ExecuteNonQueryAsync();
            }
        }

        private async Task<string> RetrieveConfiguration(ulong guildId)
        {
            using (var db = await Kerobot.GetOpenNpgsqlConnectionAsync(guildId))
            {
                using (var c = db.CreateCommand())
                {
                    c.CommandText = $"select config_json from {DBTableName} "
                        + "order by rev_id desc limit 1";
                    using (var r = await c.ExecuteReaderAsync())
                    {
                        if (await r.ReadAsync())
                        {
                            return r.GetString(0);
                        }
                        return null;
                    }
                }
            }
        }
        #endregion

        // Default guild configuration JSON is embedded in assembly. Retrieving and caching it here.
        private readonly string _defaultGuildJson;
        private string PreloadDefaultGuildJson()
        {
            const string ResourceName = "Kerobot.DefaultGuildConfig.json";

            var a = System.Reflection.Assembly.GetExecutingAssembly();
            using (var s = a.GetManifestResourceStream(ResourceName))
            using (var r = new System.IO.StreamReader(s))
                return r.ReadToEnd();
        }
    }
}
