namespace RegexBot.Services;

/// <summary>
/// Base class for services.
/// </summary>
/// <remarks>
/// Services provide core and shared functionality for this program. Modules are expected to call into services
/// directly or indirectly in order to access bot features.
/// </remarks>
internal abstract class Service {
    public RegexbotClient BotClient { get; }

    public string Name => GetType().Name;

    public Service(RegexbotClient bot) => BotClient = bot;

    /// <summary>
    /// Emits a log message.
    /// </summary>
    /// <param name="message">The log message to send. Multi-line messages are acceptable.</param>
    /// <param name="report">Specify if the log message should be sent to a reporting channel.</param>
    protected void Log(string message, bool report = false) => BotClient._svcLogging.DoLog(report, Name, message);
}
