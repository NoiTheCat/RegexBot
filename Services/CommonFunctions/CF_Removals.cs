#pragma warning disable CA1822 // "Mark members as static" - members should only be callable by code with access to this instance
using Discord.Net;

namespace RegexBot.Services.CommonFunctions;
internal partial class CommonFunctionsService : Service {
    // Hooked (indirectly)
    internal async Task<BanKickResult> BanOrKickAsync(RemovalType t, SocketGuild guild, string source, ulong target,
                                                      int banPurgeDays, string? logReason, bool sendDmToTarget) {
        if (t == RemovalType.None) throw new ArgumentException("Removal type must be 'ban' or 'kick'.");
        var dmSuccess = true;

        SocketGuildUser utarget = guild.GetUser(target);
        // Can't kick without obtaining user object. Quit here.
        if (t == RemovalType.Kick && utarget == null) return new BanKickResult(null, false, true, RemovalType.Kick, 0);

        // Send DM notification
        // Must be done before removal, or we risk not being able to send a notification afterwards
        if (sendDmToTarget) {
            if (utarget != null) dmSuccess = await BanKickSendNotificationAsync(utarget, t, logReason);
            else dmSuccess = false;
        }

        // Perform the action
        var auditReason = $"(By: {source}) {logReason}";
        try {
            if (t == RemovalType.Ban) await guild.AddBanAsync(target, banPurgeDays, auditReason);
            else await utarget!.KickAsync(auditReason);
        } catch (HttpException ex) {
            return new BanKickResult(ex, dmSuccess, false, t, target);
        }
        ModLogsProcessRemoval(guild.Id, target, t == RemovalType.Ban ? ModLogType.Ban : ModLogType.Kick, source, logReason);

        return new BanKickResult(null, dmSuccess, false, t, target);
    }

    private async Task<bool> BanKickSendNotificationAsync(SocketGuildUser target, RemovalType action, string? reason) {
        const string DMTemplate = "You have been {0} from {1}";
        const string DMTemplateReason = " for the following reason:\n{2}";

        var outMessage = string.IsNullOrWhiteSpace(reason)
            ? string.Format(DMTemplate + ".", action == RemovalType.Ban ? "banned" : "kicked", target.Guild.Name)
            : string.Format(DMTemplate + DMTemplateReason, action == RemovalType.Ban ? "banned" : "kicked", target.Guild.Name, reason);
            
        var dch = await target.CreateDMChannelAsync();
        try { await dch.SendMessageAsync(outMessage); } catch (HttpException) { return false; }
        return true;
    }
}