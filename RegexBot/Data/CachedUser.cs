using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RegexBot.Data;

[Table("cache_user")]
public class CachedUser {
    [Key]
    public long UserId { get; set; }
    public DateTimeOffset ULastUpdateTime { get; set; }
    public string Username { get; set; } = null!;
    public string Discriminator { get; set; } = null!;
    public string? AvatarUrl { get; set; }

    [InverseProperty(nameof(CachedGuildUser.User))]
    public ICollection<CachedGuildUser> Guilds { get; set; } = null!;
}
