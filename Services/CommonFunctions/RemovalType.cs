namespace RegexBot;
/// <summary>
/// Specifies possible outcomes for the removal of a user from a guild.
/// </summary>
// Despite specific to CommonFunctionsService, this enum is meant to be visible by modules too,
// thus it is placed within the root namespace.
// TODO Tends to be unused except internally. Look into removing.
public enum RemovalType {
    /// <summary>
    /// Default value. Not used in any actual circumstances.
    /// </summary>
    None,
    /// <summary>
    /// Specifies that the type of removal includes placing the user on the guild's ban list.
    /// </summary>
    Ban,
    /// <summary>
    /// Specifies that the user is removed from the server via kick.
    /// </summary>
    Kick
}
