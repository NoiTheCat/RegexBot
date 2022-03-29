using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RegexBot.Data;

[Table("cache_guilduser")]
public class CachedGuildUser {
    [Key]
    public long UserId { get; set; }
    [Key]
    public long GuildId { get; set; }
    public DateTimeOffset GULastUpdateTime { get; set; }
    public DateTimeOffset FirstSeenTime { get; set; }
    public string? Nickname { get; set; }

    [ForeignKey(nameof(CachedUser.UserId))]
    [InverseProperty(nameof(CachedUser.Guilds))]
    public CachedUser User { get; set; } = null!;
}
