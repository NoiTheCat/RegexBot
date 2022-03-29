using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace RegexBot.Data;

[Table("guild_log")]
[Index(nameof(GuildId))]
public class GuildLogLine {
    public int Id { get; set; }
    public long GuildId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Source { get; set; } = null!;
    public string Message { get; set; } = null!;
}
