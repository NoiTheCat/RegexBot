using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Kerobot
{
    /// <summary>
    /// Base class for a Kerobot module. A module implements a user-facing feature and is expected to directly handle
    /// user input (both by means of configuration and incoming Discord events) and process it accordingly.
    /// </summary>
    /// <remarks>
    /// Implementing classes should not rely on local variables to store runtime data regarding guilds.
    /// Use <see cref="CreateGuildStateAsync(JToken)"/> and <see cref="GetGuildState{T}(ulong)"/>.
    /// </remarks>
    public abstract class ModuleBase
    {
        /// <summary>
        /// Retrieves the Kerobot instance.
        /// </summary>
        public Kerobot Kerobot { get; }

        /// <summary>
        /// Retrieves the Discord client instance.
        /// </summary>
        public DiscordSocketClient DiscordClient { get => Kerobot.DiscordClient; }

        /// <summary>
        /// When a module is loaded, this constructor is called.
        /// Services are available at this point. Do not attempt to communicate to Discord within the constructor.
        /// </summary>
        public ModuleBase(Kerobot kb) => Kerobot = kb;

        /// <summary>
        /// Gets the module name.
        /// This value is derived from the class's name. It is used in configuration and logging.
        /// </summary>
        public string Name => GetType().Name;

        /// <summary>
        /// Called when a guild becomes available. The implementing class should construct an object to hold
        /// data specific to the corresponding guild for use during runtime.
        /// </summary>
        /// <param name="config">JSON token holding module configuration specific to this guild.</param>
        /// <returns>
        /// An object containing state and/or configuration information for the guild currently being processed.
        /// </returns>
        public abstract Task<object> CreateGuildStateAsync(JToken config);

        /// <summary>
        /// Retrieves the state object that corresponds with the given guild.
        /// </summary>
        /// <typeparam name="T">The state object's type.</typeparam>
        /// <param name="guildId">The guild ID for which to retrieve the state object.</param>
        /// <returns>The state object cast in the given type, or Default(T) if none exists.</returns>
        /// <exception cref="InvalidCastException">
        /// Thrown if the stored state object cannot be cast as specified.
        /// </exception>
        [DebuggerStepThrough]
        protected T GetGuildState<T>(ulong guildId) => Kerobot.GetGuildState<T>(guildId, GetType());

        /// <summary>
        /// Appends a message to the global instance log. Use sparingly.
        /// </summary>
        /// <param name="report">
        /// Specifies if the log message should be sent to the reporting channel.
        /// Only messages of very high importance should use this option.
        /// </param>
        protected Task LogAsync(string message, bool report = false) => Kerobot.InstanceLogAsync(report, Name, message);

        /// <summary>
        /// Appends a message to the log for the specified guild.
        /// </summary>
        protected Task LogAsync(ulong guild, string message) => Kerobot.GuildLogAsync(guild, Name, message);
    }

    /// <summary>
    /// Represents errors that occur when a module attempts to create a new guild state object.
    /// </summary>
    public class ModuleLoadException : Exception
    {
        /// <summary>
        /// Initializes this exception class with the specified error message.
        /// </summary>
        /// <param name="message"></param>
        public ModuleLoadException(string message) : base(message) { }
    }
}
