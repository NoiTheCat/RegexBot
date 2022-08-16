using Discord.Net;

namespace RegexBot;
/// <summary>
/// Contains information on success/failure outcomes for a warn operation.
/// </summary>
public class LogAppendResult {
    private readonly int _logId;
    private readonly string _rptDisplayName;

    /// <summary>
    /// Gets the exception thrown, if any, when attempting to send the warning to the target.
    /// </summary>
    public HttpException? MessageSendError { get; }

    /// <summary>
    /// Indicates if the operation failed due to being unable to find the user.
    /// </summary>
    public bool ErrorNotFound => MessageSendError?.HttpCode == System.Net.HttpStatusCode.NotFound;

    /// <summary>
    /// Indicates if the operation failed due to a permissions issue.
    /// </summary>
    public bool ErrorForbidden => MessageSendError?.HttpCode == System.Net.HttpStatusCode.Forbidden;

    /// <summary>
    /// Indicates if the operation completed successfully.
    /// </summary>
    public bool Success => MessageSendError == null;

    internal LogAppendResult(HttpException? error, int logId, string reportDispName) {
        _logId = logId;
        MessageSendError = error;
        _rptDisplayName = reportDispName;
    }

    /// <summary>
    /// Returns a message representative of this result that may be posted as-is
    /// within a Discord channel.
    /// </summary>
    public string GetResultString() {
        var msg = $":white_check_mark: Warning \\#{_logId} logged for {_rptDisplayName}.";
        if (!Success) msg += "\n:warning: **User did not receive warning message.** This must be discussed manually.";
        return msg;
    }
}