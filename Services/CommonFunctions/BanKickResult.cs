using Discord.Net;
using RegexBot.Common;

namespace RegexBot;
/// <summary>
/// Contains information on various success/failure outcomes for a ban or kick operation.
/// </summary>
public class BanKickResult {
    private readonly bool _userNotFound; // possible to receive this error by means other than exception
    private readonly RemovalType _rptRemovalType;
    private readonly ulong _rptTargetId;

    /// <summary>
    /// Gets a value indicating whether the kick or ban succeeded.
    /// </summary>
    public bool OperationSuccess {
        get {
            if (ErrorNotFound) return false;
            if (OperationError != null) return false;
            return true;
        }
    }

    /// <summary>
    /// The exception thrown, if any, when attempting to kick or ban the target.
    /// </summary>
    public HttpException? OperationError { get; }

    /// <summary>
    /// Indicates if the operation failed due to being unable to find the user.
    /// </summary>
    /// <remarks>
    /// This may return true even if <see cref="OperationError"/> returns null.
    /// This type of error may appear in cases that do not involve an exception being thrown.
    /// </remarks>
    public bool ErrorNotFound {
        get {
            if (_userNotFound) return true;
            return OperationError?.HttpCode == System.Net.HttpStatusCode.NotFound;
        }
    }

    /// <summary>
    /// Indicates if the operation failed due to a permissions issue.
    /// </summary>
    public bool ErrorForbidden {
        get {
            if (OperationSuccess) return false;
            return OperationError?.HttpCode == System.Net.HttpStatusCode.Forbidden;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the user was able to receive the ban or kick message.
    /// </summary>
    /// <value>
    /// <see langword="false"/> if an error was encountered when attempting to send the target a DM. Will always
    /// return <see langword="true"/> otherwise, including cases in which no message was sent.
    /// </value>
    public bool MessageSendSuccess { get; }

    internal BanKickResult(HttpException? error, bool notificationSuccess, bool errorNotFound,
        RemovalType rtype, ulong rtarget) {
        OperationError = error;
        MessageSendSuccess = notificationSuccess;
        _userNotFound = errorNotFound;
        _rptRemovalType = rtype;
        _rptTargetId = rtarget;
    }

    /// <summary>
    /// Returns a message representative of the ban/kick result that may be posted as-is
    /// within the a Discord channel.
    /// </summary>
    public string GetResultString(RegexbotClient bot) {
        string msg;

        if (OperationSuccess) msg = ":white_check_mark: ";
        else msg = ":x: Failed to ";

        if (_rptRemovalType == RemovalType.Ban) {
            if (OperationSuccess) msg += "Banned";
            else msg += "ban";
        } else if (_rptRemovalType == RemovalType.Kick) {
            if (OperationSuccess) msg += "Kicked";
            else msg += "kick";
        } else {
            throw new InvalidOperationException("Cannot create a message for removal type of None.");
        }

        if (_rptTargetId != 0) {
            var user = bot.EcQueryUser(_rptTargetId.ToString());
            if (user != null) {
                // TODO sanitize possible formatting characters in display name
                msg += $" user **{user.Username}#{user.Discriminator}**";
            } else {
                msg += $" user with ID **{_rptTargetId}**";
            }
        }

        if (OperationSuccess) {
            msg += ".";
            if (!MessageSendSuccess) msg += "\n(User was unable to receive notification message.)";
        } else {
            if (ErrorNotFound) msg += ": The specified user could not be found.";
            else if (ErrorForbidden) msg += ": " + Messages.ForbiddenGenericError;
        }

        return msg;
    }
}
