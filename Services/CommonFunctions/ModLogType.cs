namespace RegexBot;
/// <summary>
/// Specifies the type of action or event represented by a
/// <see cref="Data.ModLogEntry"/> or <see cref="LogAppendResult"/>.
/// </summary>
public enum ModLogType {
    /// <summary>
    /// An unspecified logging type.
    /// </summary>
    Other,
    /// <summary>
    /// A note appended to a user's log for moderator reference.
    /// </summary>
    Note,
    /// <summary>
    /// A warning. Similar to a note, but with higher priority and presented to the user when issued.
    /// </summary>
    Warn,
    /// <summary>
    /// A timeout, preventing the user from speaking for some amount of time.
    /// </summary>
    Timeout,
    /// <summary>
    /// A forced removal from the server.
    /// </summary>
    Kick,
    /// <summary>
    /// A forced removal from the server, with the user additionally getting added to the ban list.
    /// </summary>
    Ban
}