namespace RegexBot;
/// <summary>
/// Contains information on success or failure outcomes for certain operations.
/// </summary>
public interface IOperationResult {
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    /// <remarks>
    /// Be aware this value may return <see langword="true"/> while
    /// <see cref="NotificationSuccess"/> returns <see langword="false"/>.
    /// </remarks>
    bool Success { get; }

    /// <summary>
    /// The exception thrown, if any, when attempting to perform the operation.
    /// </summary>
    Exception? Error { get; }

    /// <summary>
    /// Indicates if the operation failed due to being unable to find the user.
    /// </summary>
    bool ErrorNotFound { get; }

    /// <summary>
    /// Indicates if the operation failed due to a permissions issue.
    /// </summary>
    bool ErrorForbidden { get; }

    /// <summary>
    /// Indicates if user DM notification for this event was successful.
    /// Always returns <see langword="true"/> in cases where no notification was requested.
    /// </summary>
    bool NotificationSuccess { get; }

    /// <summary>
    /// Returns a message representative of this result that may be posted as-is within a Discord channel.
    /// </summary>
    string ToResultString();
}