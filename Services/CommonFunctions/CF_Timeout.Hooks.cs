namespace RegexBot;
partial class RegexbotClient {
    /// <summary>
    /// Sets a timeout on a user while also adding an entry to the moderation log and attempting to notify the user.
    /// </summary>
    /// <param name="guild">The guild which the target user is associated.</param>
    /// <param name="source">
    /// The the entity which issued this log item.
    /// If it was a user, this value preferably is in the <seealso cref="Common.EntityName"/> format.
    /// </param>
    /// <param name="target">The user to be issued a timeout.</param>
    /// <param name="duration">The duration of the timeout.</param>
    /// <param name="reason">Reason for the action. Sent to the Audit Log and user (if specified).</param>
    /// <param name="sendNotificationDM">Specify whether to send a direct message to the target user informing them of the action.</param>
    public Task<TimeoutSetResult> SetTimeoutAsync(SocketGuild guild, string source, SocketGuildUser target,
                                                  TimeSpan duration, string? reason, bool sendNotificationDM)
        => _svcCommonFunctions.SetTimeoutAsync(guild, source, target, duration, reason, sendNotificationDM);
}