using Discord;
using Discord.Webhook;
using RegexBot.Data;

namespace RegexBot.Services.Logging;

/// <summary>
/// Implements logging. Logging is distinguished into two types: Instance and per-guild.
/// For further information on log types, see documentation under <see cref="Data.BotDatabaseContext"/>.
/// </summary>
class LoggingService : Service {
    // NOTE: Service.Log's functionality is implemented here. DO NOT use within this class.
    private readonly DiscordWebhookClient _instLogWebhook;

    internal LoggingService(RegexbotClient bot) : base(bot) {
        _instLogWebhook = new DiscordWebhookClient(bot.Config.InstanceLogTarget);

        // Discord.Net log handling (client logging option is specified in Program.cs)
        bot.DiscordClient.Log += DiscordClient_Log;
        // Let's also do the ready message
        bot.DiscordClient.Ready +=
            delegate { DoInstanceLog(true, nameof(RegexBot), "Connected and ready."); return Task.CompletedTask; };
    }

    /// <summary>
    /// Discord.Net logging events handled here.
    /// Only events with high importance are stored. Others are just printed to console.
    /// </summary>
    private Task DiscordClient_Log(LogMessage arg) {
        bool important = arg.Severity != LogSeverity.Info;
        string msg = $"[{Enum.GetName(typeof(LogSeverity), arg.Severity)}] {arg.Message}";
        const string logSource = "Discord.Net";
        if (arg.Exception != null) msg += "\n```\n" + arg.Exception.ToString() + "\n```";

        if (important) DoInstanceLog(true, logSource, msg);
        else ToConsole(logSource, msg);

        return Task.CompletedTask;
    }

    private static void ToConsole(string source, string message) {
        message ??= "(null)";
        var prefix = $"[{DateTimeOffset.UtcNow:u}] [{source}] ";
        foreach (var line in message.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)) {
            Console.WriteLine(prefix + line);
        }
    }

    // Hooked
    internal void DoInstanceLog(bool report, string source, string? message) {
        message ??= "(null)";
        ToConsole(source, message);

        if (report) Task.Run(() => ReportInstanceWebhook(source, message));
    }

    private async Task ReportInstanceWebhook(string source, string message) {
        try {
            EmbedBuilder e = new() {
                Footer = new EmbedFooterBuilder() { Text = source },
                Timestamp = DateTimeOffset.UtcNow,
                Description = message
            };
            await _instLogWebhook.SendMessageAsync(embeds: new[] { e.Build() });
        } catch (Discord.Net.HttpException ex) {
            DoInstanceLog(false, Name, "Failed to send message to reporting channel: " + ex.Message);
        }
    }

    // Hooked
    public void DoGuildLog(ulong guild, string source, string message) {
        message ??= "(null)";
        try {
            using var db = new BotDatabaseContext();
            db.Add(new GuildLogLine() { GuildId = (long)guild, Source = source, Message = message });
            db.SaveChanges();
#if DEBUG
            ToConsole($"DEBUG {guild} - {source}", message);
#endif
        } catch (Exception ex) {
            // Stack trace goes to console only.
            DoInstanceLog(false, Name, "Error when storing guild log line: " + ex.ToString());
        }
    }
}
