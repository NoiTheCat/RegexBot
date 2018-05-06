using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Kerobot
{
    /// <summary>
    /// Program startup class. Does initialization before starting the Discord client.
    /// </summary>
    class Program
    {
        static DateTimeOffset _startTime;
        /// <summary>
        /// Timestamp specifying the date and time that the program began running.
        /// </summary> 
        public static DateTimeOffset StartTime => _startTime;
        
        static Kerobot _main;
        
        static async Task Main(string[] args)
        {
            _startTime = DateTimeOffset.UtcNow;
            Console.WriteLine("Bot start time: " + _startTime.ToString("u"));

            // Get instance config figured out
            var opts = Options.ParseOptions(args); // Program can exit here.
            InstanceConfig cfg;
            try
            {
                cfg = new InstanceConfig(opts);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.ExitCode = 1;
                return;
            }

            // Quick test if database configuration works
            try
            {
                using (var d = new Npgsql.NpgsqlConnection(cfg.PostgresConnString))
                {
                    await d.OpenAsync();
                    d.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not establish a database connection! Check your settings and try again.");
                Console.WriteLine($"Error: {ex.GetType().FullName}: {ex.Message}");
                Environment.Exit(1);
            }

            // Configure Discord client
            var client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                DefaultRetryMode = RetryMode.AlwaysRetry,
                MessageCacheSize = 0 // using our own
            });

            // Kerobot class initialization - will set up services and modules
            _main = new Kerobot(cfg, client);

            // Set up application close handler
            Console.CancelKeyPress += Console_CancelKeyPress;

            // TODO Set up unhandled exception handler
            // send error notification to instance log channel, if possible

            // And off we go.
            await _main.DiscordClient.LoginAsync(Discord.TokenType.Bot, cfg.BotToken);
            await _main.DiscordClient.StartAsync();
            await Task.Delay(-1);
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            // TODO finish implementation when logging is set up
            e.Cancel = true;
            // _main.Log("Received Cancel event. Application will shut down...");
            // stop periodic task processing - wait for current run to finish if executing (handled by service?)
            // notify services of shutdown
            bool success = _main.DiscordClient.LogoutAsync().Wait(10000);
            // if (!success) _main.Log("Failed to disconnect cleanly from Discord. Will force shut down.");
            Environment.Exit(0);
        }
    }
}
