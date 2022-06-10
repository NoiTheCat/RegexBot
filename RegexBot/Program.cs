using Discord;
using Discord.WebSocket;

namespace RegexBot;
class Program {
    /// <summary>
    /// Timestamp specifying the date and time that the program began running.
    /// </summary> 
    public static DateTimeOffset StartTime { get; private set; }

    static RegexbotClient _main = null!;

    static async Task Main(string[] args) {
        StartTime = DateTimeOffset.UtcNow;
        Console.WriteLine("Bot start time: " + StartTime.ToString("u"));

        InstanceConfig cfg;
        try {
            cfg = new InstanceConfig(args); // Program may exit within here.
        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
            Environment.ExitCode = 1;
            return;
        }

        // Configure Discord client
        var client = new DiscordSocketClient(new DiscordSocketConfig() {
            DefaultRetryMode = RetryMode.Retry502 | RetryMode.RetryTimeouts,
            MessageCacheSize = 0, // using our own
            LogLevel = LogSeverity.Info,
            GatewayIntents = GatewayIntents.All & ~GatewayIntents.GuildPresences,
            LogGatewayIntentWarnings = false,
            AlwaysDownloadUsers = true
        });

        // Kerobot class initialization - will set up services and modules
        _main = new RegexbotClient(cfg, client);

        // Set up application close handler
        Console.CancelKeyPress += Console_CancelKeyPress;

        // TODO Set up unhandled exception handler
        // send error notification to instance log channel, if possible

        // And off we go.
        await _main.DiscordClient.LoginAsync(TokenType.Bot, cfg.BotToken);
        await _main.DiscordClient.StartAsync();
        await Task.Delay(-1);
    }

    private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e) {
        e.Cancel = true;

        _main._svcLogging.DoLog(true, nameof(RegexBot), "Shutting down. Reason: Interrupt signal.");

        var finishingTasks = Task.Run(async () => {
            // TODO periodic task service: stop processing, wait for all tasks to finish
            // TODO notify services of shutdown
            await _main.DiscordClient.StopAsync();
        });
        
        if (!finishingTasks.Wait(5000))
            _main._svcLogging.DoLog(false, nameof(RegexBot), "Could not disconnect properly. Exiting...");
        Environment.Exit(0);
    }
}
