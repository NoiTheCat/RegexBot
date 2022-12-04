﻿using System.ComponentModel.DataAnnotations.Schema;

namespace RegexBot.Data;
/// <summary>
/// Represents an item in the guild user cache.
/// </summary>
[Table("cache_usersinguild")]
public class CachedGuildUser {
    /// <summary>
    /// Gets the associated guild's snowflake ID.
    /// </summary>
    public ulong GuildId { get; set; }

    /// <inheritdoc cref="CachedUser.UserId"/>
    public ulong UserId { get; set; }

    /// <inheritdoc cref="CachedUser.ULastUpdateTime"/>
    public DateTimeOffset GULastUpdateTime { get; set; }

    /// <summary>
    /// Gets the timestamp showing when this cache entry was first added into the database.
    /// </summary>
    public DateTimeOffset FirstSeenTime { get; set; }

    /// <summary>
    /// Gets the user's cached nickname in the guild.
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// Gets the associated <seealso cref="CachedUser"/> for this entry. This entity is auto-included.
    /// </summary>
    public CachedUser User { get; set; } = null!;

    /// <summary>
    /// If included in the query, references all <seealso cref="ModLogEntry"/> items associated with this entry.
    /// </summary>
    public ICollection<ModLogEntry> Logs { get; set; } = null!;

    /// <summary>
    /// If included in the query, references all <seealso cref="CachedGuildMessage"/> items associated with this entry.
    /// </summary>
    public ICollection<CachedGuildMessage> Messages { get; set; } = null!;
}
