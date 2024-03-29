using RegexBot.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RegexBot.Data;
/// <summary>
/// Represents a moderation log entry.
/// </summary>
[Table("modlogs")]
public class ModLogEntry : ISharedEvent {
    /// <summary>
    /// Gets the ID number for this entry.
    /// </summary>
    [Key]
    public int LogId { get; set; }

    /// <summary>
    /// Gets the date and time when this entry was logged.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <inheritdoc cref="CachedGuildUser.GuildId"/>
    public long GuildId { get; set; }

    /// <summary>
    /// Gets the ID of the users for which this log entry pertains.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Gets the type of log message this represents.
    /// </summary>
    public ModLogType LogType { get; set; }

    /// <summary>
    /// Gets the the entity which issued this log item.
    /// If it was a user, this value preferably is in the <seealso cref="EntityName"/> format.
    /// </summary>
    public string IssuedBy { get; set; } = null!;

    /// <summary>
    /// Gets any additional message associated with this log entry.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// If included in the query, gets the associated <seealso cref="CachedGuildUser"/> for this entry.
    /// </summary>
    public CachedGuildUser User { get; set; } = null!;
}