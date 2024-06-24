namespace RegexBot.Services;
/// <summary>
/// Base class for services.
/// </summary>
/// <remarks>
/// Services provide core and shared functionality for this program. Modules are expected to call into services
/// directly or indirectly in order to access bot features.
/// </remarks>
internal abstract class Service(RegexbotClient bot) {
    public RegexbotClient BotClient { get; } = bot;

    public string Name => GetType().Name;

    /// <summary>
    /// Emits a log message.
    /// </summary>
    /// <param name="message">The log message to send. Multi-line messages are acceptable.</param>
    protected void Log(string message) => BotClient._svcLogging.DoLog(Name, message);
}
