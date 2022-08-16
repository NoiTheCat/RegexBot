using Discord.Net;

namespace RegexBot.Services.CommonFunctions;
internal partial class CommonFunctionsService : Service {
    // Hooked (indirectly)
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
    internal async Task<BanKickResult> BanOrKickAsync(RemovalType t,
                                                      SocketGuild guild,
                                                      string source,
                                                      ulong target,
                                                      int banPurgeDays,
                                                      string? logReason,
                                                      bool sendDmToTarget) {
        if (t == RemovalType.None) throw new ArgumentException("Removal type must be 'ban' or 'kick'.");
        var dmSuccess = true;

        SocketGuildUser utarget = guild.GetUser(target);
        // Can't kick without obtaining user object. Quit here.
        if (t == RemovalType.Kick && utarget == null) return new BanKickResult(null, false, true, RemovalType.Kick, 0);

        // TODO notify services here as soon as we get some who will want to listen to this (use source parameter)

        // Send DM notification
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

        return new BanKickResult(null, dmSuccess, false, t, target);
    }

    private static async Task<bool> BanKickSendNotificationAsync(SocketGuildUser target, RemovalType action, string? reason) {
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