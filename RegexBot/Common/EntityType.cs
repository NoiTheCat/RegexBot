namespace RegexBot.Common;

/// <summary>
/// The type of entity specified in an <see cref="EntityName"/>.
/// </summary>
public enum EntityType {
    /// <summary>Default value. Is never referenced in regular usage.</summary>
    Unspecified,
    Role,
    Channel,
    User
}
