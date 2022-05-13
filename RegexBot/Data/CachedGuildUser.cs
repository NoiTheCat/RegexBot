using System.ComponentModel.DataAnnotations.Schema;

namespace RegexBot.Data;

[Table("cache_userguild")]
public class CachedGuildUser {
    public long UserId { get; set; }
    public long GuildId { get; set; }
    public DateTimeOffset GULastUpdateTime { get; set; }
    public DateTimeOffset FirstSeenTime { get; set; }
    public string? Nickname { get; set; }

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(CachedUser.Guilds))]
    public CachedUser User { get; set; } = null!;
}
