#pragma warning disable CA1822 // "Mark members as static" - members should only be callable by code with access to this instance
using Discord.Net;
using RegexBot.Data;

namespace RegexBot.Services.CommonFunctions;
internal partial class CommonFunctionsService : Service {
    // Hooked
    internal async Task<TimeoutSetResult> SetTimeoutAsync(SocketGuild guild, string source, SocketGuildUser target,
                                                          TimeSpan duration, string? reason, bool sendNotificationDM) {
        if (duration < TimeSpan.FromMinutes(1))
            return new TimeoutSetResult(new ArgumentOutOfRangeException(
                nameof(duration), "Cannot set a timeout with a duration less than 60 seconds."), true, target);
        if (duration > TimeSpan.FromDays(28))
            return new TimeoutSetResult(new ArgumentOutOfRangeException(
                nameof(duration), "Cannot set a timeout with a duration greater than 28 days."), true, target);
        if (target.TimedOutUntil != null && DateTimeOffset.UtcNow < target.TimedOutUntil)
            return new TimeoutSetResult(new InvalidOperationException(
                "Cannot set a timeout. The user is already timed out."), true, target);

        Discord.RequestOptions? audit = null;
        if (reason != null) audit = new() { AuditLogReason = reason };
        try {
            await target.SetTimeOutAsync(duration, audit);
        } catch (HttpException ex) {
            return new TimeoutSetResult(ex, false, target);
        }
        var entry = new ModLogEntry() {
            GuildId = guild.Id,
            UserId = target.Id,
            LogType = ModLogType.Timeout,
            IssuedBy = source,
            Message = $"Duration: {Math.Floor(duration.TotalMinutes)}min{(reason == null ? "." : " - " + reason)}"
        };
        using (var db = new BotDatabaseContext()) {
            db.Add(entry);
            await db.SaveChangesAsync();
        }
        // TODO check if this log entry should be propagated now or if (to be implemented) will do it for us later
        await BotClient.PushSharedEventAsync(entry); // Until then, we for sure propagate our own

        bool dmSuccess;
        // DM notification
        if (sendNotificationDM) {
            dmSuccess = await SendUserTimeoutNoticeAsync(target, duration, reason);
        } else dmSuccess = true;

        return new TimeoutSetResult(null, dmSuccess, target);
    }

    internal async Task<bool> SendUserTimeoutNoticeAsync(SocketGuildUser target, TimeSpan duration, string? reason) {
        // you have been issued a timeout in x.
        // the timeout will expire on <t:...>
        const string DMTemplate1 = "You have been issued a timeout in {0}";
        const string DMTemplateReason = " for the following reason:\n{2}";
        const string DMTemplate2 = "\nThe timeout will expire on <t:{1}:f> (<t:{1}:R>).";

        var expireTime = (DateTimeOffset.UtcNow + duration).ToUnixTimeSeconds();
        var outMessage = string.IsNullOrWhiteSpace(reason)
            ? string.Format($"{DMTemplate1}.{DMTemplate2}", target.Guild.Name, expireTime)
            : string.Format($"{DMTemplate1}{DMTemplateReason}\n{DMTemplate2}", target.Guild.Name, expireTime, reason);

        var dch = await target.CreateDMChannelAsync();
        try { await dch.SendMessageAsync(outMessage); } catch (HttpException) { return false; }
        return true;
    }
}