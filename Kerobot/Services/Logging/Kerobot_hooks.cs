using Kerobot.Services.Logging;
using System.Threading.Tasks;

namespace Kerobot
{
    partial class Kerobot
    {
        LoggingService _svcLogging;

        /// <summary>
        /// Appends a log message to the instance log.
        /// </summary>
        /// <param name="report">Specifies if the message should be sent to the dedicated logging channel on Discord.</param>
        /// <param name="source">Name of the subsystem from which the log message originated.</param>
        /// <param name="message">The log message to append. Multi-line messages are acceptable.</param>
        public Task InstanceLogAsync(bool report, string source, string message)
            => _svcLogging.DoInstanceLogAsync(report, source, message);

        /// <summary>
        /// Appends a log message to the guild-specific log.
        /// </summary>
        /// <param name="guild">The guild ID associated with this message.</param>
        /// <param name="source">Name of the subsystem from which the log message originated.</param>
        /// <param name="message">The log message to append. Multi-line messages are acceptable.</param>
        public Task GuildLogAsync(ulong guild, string source, string message)
            => _svcLogging.DoGuildLogAsync(guild, source, message);
    }
}
