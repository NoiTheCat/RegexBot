using RegexBot.Common;
using RegexBot.Data;

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

        await reportChannel.SendMessageAsync(embed: entry.BuildEmbed(Bot));
    }
}