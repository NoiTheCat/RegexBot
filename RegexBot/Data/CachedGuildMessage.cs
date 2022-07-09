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

    [ForeignKey(nameof(AuthorId))]
    [InverseProperty(nameof(CachedUser.GuildMessages))]
    public CachedUser Author { get; set; } = null!;

    internal new CachedGuildMessage MemberwiseClone() => (CachedGuildMessage)base.MemberwiseClone();
}
