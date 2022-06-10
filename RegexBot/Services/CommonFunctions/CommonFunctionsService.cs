using Discord.Net;
using Discord.WebSocket;

namespace RegexBot.Services.CommonFunctions;

/// <summary>
/// Implements certain common actions that modules may want to perform. Using this service to perform those
/// functions may help enforce a sense of consistency across modules when performing common actions, and may
/// inform services which provide any additional features the ability to respond to those actions ahead of time.
/// 
/// This is currently an experimental section. If it turns out to not be necessary, this service will be removed and
/// modules may resume executing common actions on their own.
/// </summary>
internal class CommonFunctionsService : Service {
    public CommonFunctionsService(RegexbotClient bot) : base(bot) { }

    #region Guild member removal
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
            // TODO for kick: Figure out a way to specify invoker properly in audit log (as in mee6, etc).
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
    #endregion
}
