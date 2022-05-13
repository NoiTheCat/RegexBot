using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RegexBot.Data;
[Table("cache_messages")]
public class CachedGuildMessage {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long MessageId { get; set; }

    public long AuthorId { get; set; }

    public long GuildId { get; set; }

    public long ChannelId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? EditedAt { get; set; }

    public List<string> AttachmentNames { get; set; } = null!;

    public string Content { get; set; } = null!;

    /// <summary>Gets the timestamp when the message was last updated.</summary>
    /// <remarks>
    /// This is equivalent to coalescing the value of <see cref="EditedAt"/> and <see cref="CreatedAt"/>.
    /// </remarks>
    [NotMapped]
    public DateTimeOffset LastUpdatedAt => EditedAt ?? CreatedAt;

    [ForeignKey(nameof(AuthorId))]
    [InverseProperty(nameof(CachedUser.GuildMessages))]
    public CachedUser Author { get; set; } = null!;

    internal new CachedGuildMessage MemberwiseClone() => (CachedGuildMessage)base.MemberwiseClone();
}
