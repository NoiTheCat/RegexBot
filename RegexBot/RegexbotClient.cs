using Discord.WebSocket;
using RegexBot.Services;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RegexBot
{
    /// <summary>
    /// Kerobot main class, and the most accessible and useful class in the whole program.
    /// Provides an interface for any part of the program to call into all existing services.
    /// </summary>
    public partial class RegexbotClient
    {
        /// <summary>
        /// Gets application instance configuration.
        /// </summary>
        internal InstanceConfig Config { get; }

        /// <summary>
        /// Gets the Discord client instance.
        /// </summary>
        public DiscordSocketClient DiscordClient { get; }

        /// <summary>
        /// Gets all loaded services in an iterable form.
        /// </summary>
        internal IReadOnlyCollection<Service> Services { get; }

        /// <summary>
        /// Gets all loaded modules in an iterable form.
        /// </summary>
        internal IReadOnlyCollection<ModuleBase> Modules { get; }

        internal RegexbotClient(InstanceConfig conf, DiscordSocketClient client)
        {
            Config = conf;
            DiscordClient = client;
            
            // Get all services started up
            Services = InitializeServices();

            // Load externally defined functionality
            Modules = ModuleLoader.Load(Config, this);

            // Everything's ready to go. Print the welcome message here.
            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            InstanceLogAsync(false, nameof(RegexBot),
                $"This is RegexBot v{ver.ToString(3)}. https://github.com/NoiTheCat/RegexBot").Wait();

            // We return to Program.cs at this point.
        }

        private IReadOnlyCollection<Service> InitializeServices()
        {
            var svcList = new List<Service>();

            // Put services here as they become usable.
            _svcLogging = new Services.EventLogging.EventLoggingService(this);
            svcList.Add(_svcLogging);
            _svcGuildState = new Services.GuildState.GuildStateService(this);
            svcList.Add(_svcGuildState);
            _svcCommonFunctions = new Services.CommonFunctions.CommonFunctionsService(this);
            svcList.Add(_svcCommonFunctions);
            _svcEntityCache = new Services.EntityCache.EntityCacheService(this);
            svcList.Add(_svcEntityCache);

            return svcList.AsReadOnly();
        }

        /// <summary>
        /// Returns an open NpgsqlConnection instance.
        /// </summary>
        /// <param name="guild">
        /// If manipulating guild-specific information, this parameter sets the database connection's search path.
        /// </param>
        internal async Task<NpgsqlConnection> GetOpenNpgsqlConnectionAsync()
        {
            var db = new NpgsqlConnection(Config.PostgresConnString);
            await db.OpenAsync();
            return db;
        }
    }
}
