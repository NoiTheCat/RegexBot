using Discord.WebSocket;
using Kerobot.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static Kerobot.Kerobot;

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
        /// <param name="guildID">Corresponding guild ID for the state data being used. Can be useful when reloading.</param>
        /// <param name="config">JSON token holding module configuration specific to this guild.</param>
        /// <returns>
        /// An object containing state and/or configuration information for the guild currently being processed.
        /// </returns>
        public abstract Task<object> CreateGuildStateAsync(ulong guildID, JToken config);

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

        /// <summary>
        /// Attempts to ban the given user from the specified guild. It is greatly preferred to call this method
        /// instead of manually executing the equivalent method found in Discord.Net. It notifies other services
        /// that the action originated from the bot, and allows them to handle the action appropriately.
        /// </summary>
        /// <returns>A structure containing results of the ban operation.</returns>
        /// <param name="guild">The guild in which to attempt the action.</param>
        /// <param name="source">The user, module, or service which is requesting this action to be taken.</param>
        /// <param name="targetUser">The user which to perform the action to.</param>
        /// <param name="purgeDays">Number of days of prior post history to delete on ban. Must be between 0-7.</param>
        /// <param name="reason">Reason for the action. Sent to the Audit Log and user (if specified).</param>
        /// <param name="dmMsg">
        /// Message to DM the target user. Set null to disable. Instances of "%s" are replaced with the guild name
        /// and instances of "%r" are replaced with the reason.
        /// </param>
        protected Task<BanKickResult> BanAsync(SocketGuild guild, string source, ulong targetUser, int purgeDays, string reason, string dmMsg)
            => Kerobot.BanOrKickAsync(RemovalType.Ban, guild, source, targetUser, purgeDays, reason, dmMsg);

        /// <summary>
        /// Similar to <see cref="BanAsync(SocketGuild, string, ulong, int, string, string)"/>, but making use of an
        /// EntityCache lookup to determine the target.
        /// </summary>
        /// <param name="targetSearch">The EntityCache search string.</param>
        protected async Task<BanKickResult> BanAsync(SocketGuild guild, string source, string targetSearch, int purgeDays, string reason, string dmMsg)
        {
            var result = await Kerobot.EcQueryUser(guild.Id, targetSearch);
            if (result == null) return new BanKickResult(null, false, true);
            return await BanAsync(guild, source, result.UserID, purgeDays, reason, dmMsg);
        }

        /// <summary>
        /// Attempts to ban the given user from the specified guild. It is greatly preferred to call this method
        /// instead of manually executing the equivalent method found in Discord.Net. It notifies other services
        /// that the action originated from the bot, and allows them to handle the action appropriately.
        /// </summary>
        /// <returns>A structure containing results of the ban operation.</returns>
        /// <param name="guild">The guild in which to attempt the action.</param>
        /// <param name="source">The user, if any, which requested the action to be taken.</param>
        /// <param name="targetUser">The user which to perform the action to.</param>
        /// <param name="reason">Reason for the action. Sent to the Audit Log and user (if specified).</param>
        /// <param name="dmMsg">
        /// Message to DM the target user. Set null to disable. Instances of "%s" are replaced with the guild name
        /// and instances of "%r" are replaced with the reason.
        /// </param>
        protected Task<BanKickResult> KickAsync(SocketGuild guild, string source, ulong targetUser, string reason, string dmMsg)
            => Kerobot.BanOrKickAsync(RemovalType.Ban, guild, source, targetUser, 0, reason, dmMsg);

        /// <summary>
        /// Similar to <see cref="KickAsync(SocketGuild, string, ulong, string, string)"/>, but making use of an
        /// EntityCache lookup to determine the target.
        /// </summary>
        /// <param name="targetSearch">The EntityCache search string.</param>
        protected async Task<BanKickResult> KickAsync(SocketGuild guild, string source, string targetSearch, string reason, string dmMsg)
        {
            var result = await Kerobot.EcQueryUser(guild.Id, targetSearch);
            if (result == null) return new BanKickResult(null, false, true);
            return await KickAsync(guild, source, result.UserID, reason, dmMsg);
        }

        /// <summary>
        /// Returns the list of moderators defined in the current guild configuration.
        /// </summary>
        /// <returns>
        /// An <see cref="EntityList"/> with corresponding moderator configuration data.
        /// In case none exists, an empty list will be returned.
        /// </returns>
        protected EntityList GetModerators(ulong guild) => Kerobot.GetModerators(guild);
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
