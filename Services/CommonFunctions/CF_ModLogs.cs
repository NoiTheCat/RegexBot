#pragma warning disable CA1822 // "Mark members as static" - members should only be callable by code with access to this instance
using Discord.Net;
using RegexBot.Data;

namespace RegexBot.Services.CommonFunctions;
internal partial class CommonFunctionsService : Service {

    // things this should do:
    // set a note
    // set a warn (like note, but spicy)
    // -> return with a WarnLogResult? And send it down the chute...

    // Called by EF_Removals, this processes a removal into a log entry.
    // A notification for this entry is then propagated.
    private void ModLogsProcessRemoval(ulong guildId, ulong targetId, ModLogType remType, string source, string? logReason) {
        var entry = new ModLogEntry() {
            GuildId = (long)guildId,
            UserId = (long)targetId,
            LogType = remType,
            IssuedBy = source,
            Message = logReason
        };
        using (var db = new BotDatabaseContext()) {
            db.Add(entry);
            db.SaveChanges();
        }
        BotClient.PushSharedEventAsync(entry);
    }

    internal async Task<HttpException?> SendUserWarningAsync(SocketGuildUser target, string? reason) {
        const string DMTemplate = "You have been issued a warning in {0}";
        const string DMTemplateReason = " with the following message:\n{1}";

        var outMessage = string.IsNullOrWhiteSpace(reason)
            ? string.Format(DMTemplate + ".", target.Guild.Name)
            : string.Format(DMTemplate + DMTemplateReason, target.Guild.Name, reason);
        try {
            var dch = await target.CreateDMChannelAsync();
            await dch.SendMessageAsync(outMessage);
        } catch (HttpException ex) {
            return ex;
        }
        return null;
    }
}