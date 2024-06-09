using Discord;
using Microsoft.EntityFrameworkCore;
using RegexBot.Common;
using RegexBot.Data;

namespace RegexBot.Modules.ModCommands.Commands;
class ShowModLogs : CommandConfig {
    const int LogEntriesPerMessage = 10;
    private readonly string _usage;

    protected override string DefaultUsageMsg => _usage;

    // No configuration.
    // TODO bring in some options from BanKick. Particularly custom success msg.
    // TODO when ModLogs fully implemented, add a reason?
    public ShowModLogs(ModCommands module, JObject config) : base(module, config) {
        _usage = $"{Command} `user or user ID` [page]\n"
            + "Retrieves moderation log entries regarding the specified user.";
    }

    // Usage: (command) (query) [page]
    public override async Task Invoke(SocketGuild g, SocketMessage msg) {
        var line = SplitToParams(msg, 3);
        if (line.Length < 2) {
            await SendUsageMessageAsync(msg.Channel, null);
            return;
        }
        int pagenum;
        if (line.Length == 3) {
            const string PageNumError = ":x: Requested page must be a non-negative number.";
            if (!int.TryParse(line[2], out pagenum)) {
                await SendUsageMessageAsync(msg.Channel, PageNumError);
            }
            if (pagenum <= 0) await SendUsageMessageAsync(msg.Channel, PageNumError);
        } else pagenum = 1;

        var query = Module.Bot.EcQueryGuildUser(g.Id, line[1]);
        if (query == null) {
            await msg.Channel.SendMessageAsync(":x: Unable to find the given user.");
            return;
        }

        int totalPages;
        List<ModLogEntry> results;
        using (var db = new BotDatabaseContext()) {
            var totalEntries = db.ModLogs
                .Where(l => l.GuildId == query.GuildId && l.UserId == query.UserId)
                .Count();
            totalPages = (int)Math.Ceiling((double)totalEntries / LogEntriesPerMessage);
            results = [.. db.ModLogs
                .Where(l => l.GuildId == query.GuildId && l.UserId == query.UserId)
                .OrderByDescending(l => l.LogId)
                .Skip((pagenum - 1) * LogEntriesPerMessage)
                .Take(LogEntriesPerMessage)
                .AsNoTracking()];
        }

        var resultList = new EmbedBuilder() {
            Author = new EmbedAuthorBuilder() {
                Name = $"{query.User.GetDisplayableUsername()}",
                IconUrl = query.User.AvatarUrl
            },
            Footer = new EmbedFooterBuilder() {
                Text = $"Page {pagenum} of {totalPages}",
                IconUrl = Module.Bot.DiscordClient.CurrentUser.GetAvatarUrl()
            },
            Title = "Moderation logs"
        };
        foreach (var item in results) {
            var f = new EmbedFieldBuilder() {
                Name = $"{Enum.GetName(item.LogType)} \\#{item.LogId}",
                Value = $"**Timestamp**: <t:{item.Timestamp.ToUnixTimeSeconds()}:f>\n"
                    + $"**Issued by**: {Utilities.TryFromEntityNameString(item.IssuedBy, Module.Bot)}\n"
                    + $"**Message**: {item.Message ?? "*none specified*"}"
            };
            resultList.AddField(f);
        }

        await msg.Channel.SendMessageAsync(embed: resultList.Build());
    }
}