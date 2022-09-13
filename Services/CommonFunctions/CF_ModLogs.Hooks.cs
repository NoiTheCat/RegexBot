#pragma warning disable CA1822 // "Mark members as static" - members should only be callable by code with access to this instance
using Discord.Net;
using RegexBot.Common;
using RegexBot.Data;

namespace RegexBot;
partial class RegexbotClient {
    /// <summary>
    /// Appends a note to the moderation log regarding the given user, containing the given message.
    /// </summary>
    /// <remarks>
    /// Unlike warnings, notes are private and intended for moderators only. Users are never notified and may
    /// never be aware of notes associated with them. Otherwise, they function as any other entry in the log.
    /// </remarks>
    /// <param name="guild">The guild which the target user is associated.</param>
    /// <param name="targetUser">The snowflake ID of the target user.</param>
    /// <param name="source">
    /// The the entity which issued this log item.
    /// If it was a user, this value preferably is in the <seealso cref="EntityName"/> format.
    /// </param>
    /// <param name="message">The message to add to this entry.</param>
    /// <returns>
    /// The resulting <see cref="ModLogEntry"/> from the creation of this note.
    /// </returns>
    public async Task<ModLogEntry> AddUserNoteAsync(SocketGuild guild, ulong targetUser, string source, string? message) {
        var entry = new ModLogEntry() {
            GuildId = (long)guild.Id,
            UserId = (long)targetUser,
            LogType = ModLogType.Note,
            IssuedBy = source,
            Message = message
        };
        using (var db = new BotDatabaseContext()) {
            db.Add(entry);
            await db.SaveChangesAsync();
        }
        await PushSharedEventAsync(entry);
        return entry;
    }

    /// <summary>
    /// Warns a user, adding an entry to the moderation log and also attempting to notify the user.
    /// </summary>
    /// <param name="guild">The guild which the target user is associated.</param>
    /// <param name="targetUser">The snowflake ID of the target user.</param>
    /// <param name="source">
    /// The the entity which issued this log item.
    /// If it was a user, this value preferably is in the <seealso cref="EntityName"/> format.
    /// </param>
    /// <param name="message">The message to add to this entry.</param>
    /// <returns>
    /// A tuple containing the resulting <see cref="ModLogEntry"/> and <see cref="LogAppendResult"/>.
    /// </returns>
    public async Task<(ModLogEntry, LogAppendResult)> AddUserWarnAsync(SocketGuild guild, ulong targetUser, string source, string? message) {
        var entry = new ModLogEntry() {
            GuildId = (long)guild.Id,
            UserId = (long)targetUser,
            LogType = ModLogType.Warn,
            IssuedBy = source,
            Message = message
        };
        using (var db = new BotDatabaseContext()) {
            db.Add(entry);
            await db.SaveChangesAsync();
        }
        await PushSharedEventAsync(entry);

        // Attempt warning message
        var userSearch = _svcEntityCache.QueryUserCache(targetUser.ToString());
        var userDisp = userSearch != null
            ? $" user **{userSearch.Username}#{userSearch.Discriminator}**"
            : $" user with ID **{targetUser}**";
        var targetGuildUser = guild.GetUser(targetUser);
        if (targetGuildUser == null) return (entry, new LogAppendResult(
            new HttpException(System.Net.HttpStatusCode.NotFound, null), entry.LogId, userDisp));

        var sendStatus = await _svcCommonFunctions.SendUserWarningAsync(targetGuildUser, message);
        return (entry, new LogAppendResult(sendStatus, entry.LogId, userDisp));
    }
}