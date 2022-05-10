﻿using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Noikoio.RegexBot
{
    /// <summary>
    /// Main class. On start, initializes bot modules and passes the DiscordSocketClient to them
    /// </summary>
    class RegexBot
    {
        private static Configuration _config;
        private readonly DiscordSocketClient _client;
        private BotModule[] _modules;
        private readonly AsyncLogger _dlog;

        internal static Configuration Config => _config;
        internal IEnumerable<BotModule> Modules => _modules;
        
        internal RegexBot()
        {
            // Welcome message
            string name = nameof(RegexBot);
            string sv = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            Logger.GetLogger(name)
                .Invoke($"{name} v{sv} - https://github.com/Noikoio/RegexBot")
                .Wait();

            // Load configuration
            _config = new Configuration(this);
            if (!_config.LoadInitialConfig())
            {
#if DEBUG
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
#endif
                Environment.Exit(1);
            }

            // Set Discord client settings
            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Info,
                AlwaysDownloadUsers = true,
                DefaultRetryMode = RetryMode.AlwaysRetry,
                MessageCacheSize = 0,
                GatewayIntents = GatewayIntents.All & ~GatewayIntents.GuildPresences,
                LogGatewayIntentWarnings = false
            });

            // Hook up basic handlers and other references
            _client.Connected += Client_Connected;
            EntityCache.EntityCache.SetClient(_client);

            // Initialize modules
            _modules = new BotModule[]
            {
                new Module.DMLogger.DMLogger(_client),
                new Module.AutoMod.AutoMod(_client),
                new Module.ModCommands.ModCommands(_client),
                new Module.AutoRespond.AutoRespond(_client),
                new Module.EntryAutoRole.EntryAutoRole(_client),
                new Module.PendingAutoRole.PendingAutoRole(_client),
                new Module.VoiceRoleSync.VoiceRoleSync(_client),
                new Module.VoteTempChannel.VoteTempChannel(_client),

                // EntityCache loads before anything using it
                new EntityCache.ECModule(_client),
                new Module.ModLogs.ModLogs(_client)
            };

            // Set up logging
            _dlog = Logger.GetLogger("Discord.Net");
            _client.Log += Client_Log;

            // Finish loading configuration
            var conf = _config.ReloadServerConfig().Result;
            if (conf == false)
            {
                Console.WriteLine("Failed to load server configuration.");
#if DEBUG
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
#endif
                Environment.Exit(1);
            }
        }
        
        internal async Task Start()
        {
            
            await _client.LoginAsync(TokenType.Bot, Config.BotUserToken);
            await _client.StartAsync();
            
            await Task.Delay(-1);
        }

        private async Task Client_Connected() => await _client.SetGameAsync(Config.CurrentGame);

        // Defined within this class because a reference to the client is required
        public void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Logger.GetLogger("Runtime")("Caught cancel key. Will attempt to disconnect...").Wait();
            _client.LogoutAsync().Wait();
            _client.Dispose();
#if DEBUG
            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
#endif
            Environment.Exit(0);
        }

        public async Task Client_Log(LogMessage arg)
        {
            await _dlog(
                    String.Format("{0}: {1}{2}", arg.Source, ((int)arg.Severity < 3 ? arg.Severity + ": " : ""),
                    arg.Message));
            if (arg.Exception != null) await _dlog(arg.Exception.ToString());
        }
    }
}
