using Discord;
using Discord.WebSocket;
using Kerobot.Services;
using Npgsql;
using System;
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
        // This is to prevent this file from having too many references to many different and unrelated features.

        private readonly InstanceConfig _icfg;
        private readonly DiscordSocketClient _client;

        /// <summary>
        /// Gets application instance configuration.
        /// </summary>
        internal InstanceConfig Config => _icfg;
        /// <summary>
        /// Gets the Discord client instance.
        /// </summary>
        public DiscordSocketClient DiscordClient => _client;

        internal Kerobot(InstanceConfig conf, DiscordSocketClient client)
        {
            _icfg = conf;
            _client = client;

            InitializeServices();
            
            // and prepare modules here
        }

        private void InitializeServices()
        {
            throw new NotImplementedException();
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
