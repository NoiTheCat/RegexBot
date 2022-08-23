namespace RegexBot.Common;
/// <summary>
/// The type of entity specified in an <see cref="EntityName"/>.
/// </summary>
public enum EntityType {
    /// <summary>
    /// Userd when the <see cref="EntityName"/> represents a role.
    /// </summary>
    Role,
    /// <summary>
    /// Used when the <see cref="EntityName"/> represents a channel.
    /// </summary>
    Channel,
    /// <summary>
    /// Used when the <see cref="EntityName"/> represents a user.
    /// </summary>
    User
}
