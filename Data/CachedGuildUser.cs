using System.ComponentModel.DataAnnotations.Schema;

namespace RegexBot.Data;
/// <summary>
/// Represents an item in the guild user cache.
/// </summary>
[Table("cache_usersinguild")]
public class CachedGuildUser {
    /// <inheritdoc cref="CachedUser.UserId"/>
    public long UserId { get; set; }

    /// <summary>
    /// Gets the associated guild's snowflake ID.
    /// </summary>
    public long GuildId { get; set; }

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
    /// If included in the query, references the associated <seealso cref="CachedUser"/> for this entry.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(CachedUser.Guilds))]
    public CachedUser User { get; set; } = null!;
}
