using System.Threading.Tasks;

namespace RegexBot.Services
{
    /// <summary>
    /// Base class for Kerobot services.
    /// </summary>
    /// <remarks>
    /// Services provide the core functionality of this program. Modules are expected to call into methods
    /// provided by services for the times when processor-intensive or shared functionality needs to be utilized.
    /// </remarks>
    internal abstract class Service
    {
        public RegexbotClient BotClient { get; }

        public string Name => this.GetType().Name;

        public Service(RegexbotClient bot) => BotClient = bot;

        /// <summary>
        /// Creates a log message.
        /// </summary>
        /// <param name="message">Logging message contents.</param>
        /// <param name="report">Determines if the log message should be sent to a reporting channel.</param>
        /// <returns></returns>
        protected Task Log(string message, bool report = false) => BotClient.InstanceLogAsync(report, Name, message);
    }
}
