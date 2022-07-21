using Discord;
using Discord.Webhook;
using System.Reflection;
using System.Text;

namespace RegexBot.Services.Logging;
/// <summary>
/// Implements program-wide logging.
/// </summary>
class LoggingService : Service {
    // NOTE: Service.Log's functionality is implemented here. DO NOT use within this class.
    private readonly DiscordWebhookClient _instLogWebhook;
    private readonly string? _logBasePath;

    internal LoggingService(RegexbotClient bot) : base(bot) {
        _instLogWebhook = new DiscordWebhookClient(bot.Config.InstanceLogTarget);
        _logBasePath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)
            + Path.DirectorySeparatorChar + "logs";
        try {
            if (!Directory.Exists(_logBasePath)) Directory.CreateDirectory(_logBasePath);
            Directory.GetFiles(_logBasePath);
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            _logBasePath = null;
            Output(Name, "Cannot create or access logging directory. File logging will be disabled.");
        }

        bot.DiscordClient.Log += DiscordClient_Log;
    }

    /// <summary>
    /// Discord.Net logging events handled here.
    /// Only events with high importance are stored. Others are just printed to console.
    /// </summary>
    private Task DiscordClient_Log(LogMessage arg) {
        var msg = $"[{Enum.GetName(typeof(LogSeverity), arg.Severity)}] {arg.Message}";
        if (arg.Exception != null) msg += "\n```\n" + arg.Exception.ToString() + "\n```";

        var important = arg.Severity != LogSeverity.Info;
        switch (arg.Message) { // Prevent webhook logs for these 'important' Discord.Net messages
            case "Connecting":
            case "Connected":
            case "Ready":
            case "Disconnecting":
            case "Disconnected":
            case "Resumed previous session":
            case "Failed to resume previous session":
                important = false;
                break;
        }
        DoLog(important, "Discord.Net", msg);

        return Task.CompletedTask;
    }

    private void Output(string source, string message) {
        var now = DateTimeOffset.UtcNow;
        var output = new StringBuilder();
        var prefix = $"[{now:u}] [{source}] ";
        foreach (var line in message.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)) {
            output.Append(prefix).AppendLine(line);
        }
        var outstr = output.ToString();
        Console.Write(outstr);
        if (_logBasePath != null) {
            var filename = _logBasePath + Path.DirectorySeparatorChar + $"{now:yyyy-MM}.log";
            File.AppendAllText(filename, outstr, Encoding.UTF8);
        }
    }

    // Hooked
    internal void DoLog(bool report, string source, string? message) {
        message ??= "(null)";
        Output(source, message);
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
        } catch (Exception ex) {
            DoLog(false, Name, "Failed to send message to reporting channel: " + ex.Message);
        }
    }
}
