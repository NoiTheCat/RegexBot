using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RegexBot.Data;
/// <summary>
/// Represents an item in the user cache.
/// </summary>
[Table("cache_users")]
public class CachedUser {
    /// <summary>
    /// Gets the user's snowflake ID.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long UserId { get; set; }

    /// <summary>
    /// Gets the timestamp showing when this cache entry was last updated.
    /// </summary>
    public DateTimeOffset ULastUpdateTime { get; set; }

    /// <summary>
    /// Gets the user's username value, without the discriminator.
    /// </summary>
    public string Username { get; set; } = null!;

    /// <summary>
    /// Gets the user's discriminator value.
    /// </summary>
    public string Discriminator { get; set; } = null!;
    
    /// <summary>
    /// Gets the avatar URL, if any, for the associated user.
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// If included in the query, gets the list of associated <seealso cref="CachedGuildUser"/> entries for this entry.
    /// </summary>
    [InverseProperty(nameof(CachedGuildUser.User))]
    public ICollection<CachedGuildUser> Guilds { get; set; } = null!;

    /// <summary>
    /// If included in the query, gets the list of associated <seealso cref="CachedGuildMessage"/> entries for this entry.
    /// </summary>
    [InverseProperty(nameof(CachedGuildMessage.Author))]
    public ICollection<CachedGuildMessage> GuildMessages { get; set; } = null!;
}
