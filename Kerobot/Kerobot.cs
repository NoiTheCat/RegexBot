using Discord.WebSocket;
using Kerobot.Services;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kerobot
{
    /// <summary>
    /// Kerobot main class, and the most accessible and useful class in the whole program.
    /// Provides an interface for any part of the program to call into all existing services.
    /// </summary>
    public partial class Kerobot
    {
        // Partial class: Services are able to add their own methods and properties to this class.
        // This is to prevent this file from having too many references to different and unrelated features.

        private readonly InstanceConfig _icfg;
        private readonly DiscordSocketClient _client;
        private IReadOnlyCollection<Service> _services;
        private IReadOnlyCollection<ModuleBase> _modules;

        /// <summary>
        /// Gets application instance configuration.
        /// </summary>
        internal InstanceConfig Config => _icfg;
        /// <summary>
        /// Gets the Discord client instance.
        /// </summary>
        public DiscordSocketClient DiscordClient => _client;
        /// <summary>
        /// All loaded services in an iterable form.
        /// </summary>
        internal IReadOnlyCollection<Service> Services => _services;
        /// <summary>
        /// All loaded modules in an iterable form.
        /// </summary>
        internal IReadOnlyCollection<ModuleBase> Modules => _modules;

        internal Kerobot(InstanceConfig conf, DiscordSocketClient client)
        {
            _icfg = conf;
            _client = client;

            // 'Ready' event handler. Because there's no other place for it.
            _client.Ready += async delegate
            {
                await InstanceLogAsync(true, "Kerobot", "Connected and ready.");
            };

            InitializeServices();

            // TODO prepare modules here

            // Everything's ready to go by now. Print the welcome message here.
            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            InstanceLogAsync(false, "Kerobot",
                $"This is Kerobot v{ver.ToString(3)}. https://github.com/Noikoio/Kerobot").Wait();
        }

        private void InitializeServices()
        {
            var svcList = new List<Service>();

            // Put services here as they become usable.
            _svcLogging = new Services.Logging.LoggingService(this);
            svcList.Add(_svcLogging);

            _services = svcList.AsReadOnly();
        }

        /// <summary>
        /// Returns an open NpgsqlConnection instance.
        /// </summary>
        /// <param name="guild">
        /// If manipulating guild-specific information, this parameter sets the database connection's search path.
        /// </param>
        internal async Task<NpgsqlConnection> GetOpenNpgsqlConnectionAsync(ulong? guild)
        {
            string cs = _icfg.PostgresConnString;
            if (guild.HasValue) cs += ";searchpath=guild_" + guild.Value;

            var db = new NpgsqlConnection(cs);
            await db.OpenAsync();
            return db;
        }
    }
}
