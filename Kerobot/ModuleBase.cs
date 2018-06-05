using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Kerobot
{
    /// <summary>
    /// Base class for a Kerobot module. A module implements a user-facing feature and is expected to directly handle
    /// user input (both by means of configuration and incoming Discord events) and process it accordingly.
    /// </summary>
    /// <remarks>
    /// Implementing classes should not rely on local/instance variables to store data. Make use of
    /// <see cref="CreateGuildStateAsync(JToken)"/> and <see cref="GetGuildState{T}(ulong)"/>.
    /// </remarks>
    public abstract class ModuleBase
    {
        private readonly Kerobot _kb;

        /// <summary>
        /// Retrieves the Kerobot instance.
        /// </summary>
        public Kerobot Kerobot => _kb;

        /// <summary>
        /// When a module is loaded, this constructor is called.
        /// Services are available at this point. Do not attempt to communicate to Discord within the constructor.
        /// </summary>
        public ModuleBase(Kerobot kb) => _kb = kb;

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
        protected T GetGuildState<T>(ulong guildId) => Kerobot.GetGuildState<T>(guildId, GetType());
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
