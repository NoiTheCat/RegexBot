using Discord.Net;
using RegexBot.Common;

namespace RegexBot;
/// <summary>
/// Contains information on various success/failure outcomes for setting a timeout.
/// </summary>
public class TimeoutSetResult : IOperationResult {
    private readonly SocketGuildUser? _target;

    /// <inheritdoc/>
    public bool Success => Error == null;

    /// <inheritdoc/>
    public Exception? Error { get; }

    /// <inheritdoc/>
    public bool ErrorNotFound => (_target == null) || ((Error as HttpException)?.HttpCode == System.Net.HttpStatusCode.NotFound);

    /// <inheritdoc/>
    public bool ErrorForbidden => (Error as HttpException)?.HttpCode == System.Net.HttpStatusCode.Forbidden;

    /// <inheritdoc/>
    public bool NotificationSuccess { get; }

    internal TimeoutSetResult(Exception? error, bool notificationSuccess, SocketGuildUser? target) {
        Error = error;
        NotificationSuccess = notificationSuccess;
        _target = target;
    }

    /// <inheritdoc/>
    public string ToResultString() {
        if (Success) {
            var msg = $":white_check_mark: Timeout set for **{_target!.Username}#{_target.Discriminator}**.";
            if (!NotificationSuccess) msg += "\n(User was unable to receive notification message.)";
            return msg;
        } else {
            var msg = ":x: Failed to set timeout: ";
            if (ErrorNotFound) msg += "The specified user could not be found.";
            else if (ErrorForbidden) msg += Messages.ForbiddenGenericError;
            else if (Error != null) msg += Error.Message;
            else msg += "Unknown error.";
            return msg;
        }
    }
}