#pragma warning disable CA1822 // "Mark members as static" - members should only be callable by code with access to this instance
using Discord.Net;

namespace RegexBot.Services.CommonFunctions;
internal partial class CommonFunctionsService : Service {
    // Hooked (indirectly)
    internal async Task<BanKickResult> BanOrKickAsync(bool isBan, SocketGuild guild, string source, ulong target,
                                                      int banPurgeDays, string? logReason, bool sendDmToTarget) {
        var dmSuccess = true;

        SocketGuildUser utarget = guild.GetUser(target);
        // Can't kick without obtaining user object. Quit here.
        if (isBan == false && utarget == null) return new BanKickResult(null, false, true, false, 0);

        // Send DM notification
        // Must be done before removal, or we risk not being able to send a notification afterwards
        if (sendDmToTarget) {
            if (utarget != null) dmSuccess = await BanKickSendNotificationAsync(utarget, isBan, logReason);
            else dmSuccess = false;
        }

        // Perform the action
        var auditReason = $"(By: {source}) {logReason}";
        try {
            if (isBan) await guild.AddBanAsync(target, banPurgeDays, auditReason);
            else await utarget!.KickAsync(auditReason);
        } catch (HttpException ex) {
            return new BanKickResult(ex, dmSuccess, false, isBan, target);
        }
        ModLogsProcessRemoval(guild.Id, target, isBan ? ModLogType.Ban : ModLogType.Kick, source, logReason);

        return new BanKickResult(null, dmSuccess, false, isBan, target);
    }

    private async Task<bool> BanKickSendNotificationAsync(SocketGuildUser target, bool isBan, string? reason) {
        const string DMTemplate = "You have been {0} from {1}";
        const string DMTemplateReason = " for the following reason:\n{2}";

        var outMessage = string.IsNullOrWhiteSpace(reason)
            ? string.Format(DMTemplate + ".", isBan ? "banned" : "kicked", target.Guild.Name)
            : string.Format(DMTemplate + DMTemplateReason, isBan ? "banned" : "kicked", target.Guild.Name, reason);
            
        var dch = await target.CreateDMChannelAsync();
        try { await dch.SendMessageAsync(outMessage); } catch (HttpException) { return false; }
        return true;
    }
}