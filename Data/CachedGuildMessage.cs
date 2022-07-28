using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RegexBot.Data;
/// <summary>
/// Represents an item in the guild message cache.
/// </summary>
[Table("cache_guildmessages")]
public class CachedGuildMessage {
    /// <summary>
    /// Gets the message's snowflake ID.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long MessageId { get; set; }

    /// <summary>
    /// Gets the message author's snowflake ID.
    /// </summary>
    public long AuthorId { get; set; }

    /// <summary>
    /// Gets the associated guild's snowflake ID.
    /// </summary>
    public long GuildId { get; set; }

    /// <summary>
    /// Gets the corresponding channel's snowflake ID.
    /// </summary>
    public long ChannelId { get; set; }

    /// <summary>
    /// Gets the timestamp showing when this message was originally created.
    /// </summary>
    /// <remarks>
    /// Though it's possible to draw this from <see cref="MessageId"/>, it is stored in the database
    /// as a separate field for any possible necessary use via database queries.
    /// </remarks>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets the timestamp, if any, showing when this message was last edited.
    /// </summary>
    public DateTimeOffset? EditedAt { get; set; }

    /// <summary>
    /// Gets a list of file names that were attached to this message.
    /// </summary>
    public List<string> AttachmentNames { get; set; } = null!;

    /// <summary>
    /// Gets this message's content.
    /// </summary>
    public string? Content { get; set; } = null!;

    /// <summary>
    /// If included in the query, references the associated <seealso cref="CachedUser"/> for this entry.
    /// </summary>
    [ForeignKey(nameof(AuthorId))]
    [InverseProperty(nameof(CachedUser.GuildMessages))]
    public CachedUser Author { get; set; } = null!;

    // Used by MessageCachingSubservice
    internal static CachedGuildMessage? Clone(CachedGuildMessage? original) {
        if (original == null) return null;
        return new() {
            MessageId = original.MessageId,
            AuthorId = original.AuthorId,
            GuildId = original.GuildId,
            ChannelId = original.ChannelId,
            CreatedAt = original.CreatedAt,
            EditedAt = original.EditedAt,
            AttachmentNames = new(original.AttachmentNames),
            Content = original.Content
        };
    }
}
