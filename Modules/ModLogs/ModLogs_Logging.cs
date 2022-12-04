using Discord;
using RegexBot.Common;
using RegexBot.Data;
using System.Text;

namespace RegexBot.Modules.ModLogs;
// Contains all logic relating to reporting new database mod log entries
internal partial class ModLogs {
    public async Task HandleLog(ModLogEntry entry) {
        var guild = Bot.DiscordClient.GetGuild((ulong)entry.GuildId);
        if (guild == null) return;
        var conf = GetGuildState<ModuleConfig>(guild.Id);
        if ((conf?.LogModLogs ?? false) == false) return;
        var reportChannel = conf?.ReportingChannel?.FindChannelIn(guild, true);
        if (reportChannel == null) return;

        await reportChannel.SendMessageAsync(embed: BuildLogEmbed(entry));
    }

    /// <summary>
    /// Builds and returns an embed which displays this log entry.
    /// </summary>
    private Embed BuildLogEmbed(ModLogEntry entry) {
        var issuedDisplay = Utilities.TryFromEntityNameString(entry.IssuedBy, bot);
        string targetDisplay;
        var targetq = Bot.EcQueryUser(entry.UserId.ToString());
        if (targetq != null) targetDisplay = $"<@{targetq.UserId}> - {targetq.Username}#{targetq.Discriminator} `{targetq.UserId}`";
        else targetDisplay = $"User with ID `{entry.UserId}`";

        var logEmbed = new EmbedBuilder()
            .WithColor(Color.DarkGrey)
            .WithTitle(Enum.GetName(typeof(ModLogType), entry.LogType) + " logged:")
            .WithTimestamp(entry.Timestamp)
            .WithFooter($"Log #{entry.LogId}", Bot.DiscordClient.CurrentUser.GetAvatarUrl()); // Escaping '#' not necessary here
        if (entry.Message != null) {
            logEmbed.Description = entry.Message;
        }

        var contextStr = new StringBuilder();
        contextStr.AppendLine($"User: {targetDisplay}");
        contextStr.AppendLine($"Logged by: {issuedDisplay}");

        logEmbed.AddField(new EmbedFieldBuilder() {
            Name = "Context",
            Value = contextStr.ToString()
        });

        return logEmbed.Build();
    }
}