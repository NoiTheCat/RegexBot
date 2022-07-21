using RegexBot.Common;
using System.Diagnostics;

namespace RegexBot;

/// <summary>
/// Base class for a RegexBot module. A module implements a user-facing feature and is expected to directly handle
/// user input (both by means of configuration and incoming Discord events) and process it accordingly.
/// </summary>
/// <remarks>
/// Implementing classes should not rely on local variables to store runtime or state data for guilds.
/// Instead, use <see cref="CreateGuildStateAsync"/> and <see cref="GetGuildState"/>.
/// </remarks>
public abstract class RegexbotModule {
    /// <summary>
    /// Retrieves the bot instance.
    /// </summary>
    public RegexbotClient Bot { get; }

    /// <summary>
    /// Retrieves the Discord client instance.
    /// </summary>
    public DiscordSocketClient DiscordClient { get => Bot.DiscordClient; }

    /// <summary>
    /// Called when a module is being loaded.
    /// At this point, all bot services are available, but Discord is not. Do not use <see cref="DiscordClient"/>.
    /// </summary>
    public RegexbotModule(RegexbotClient bot) => Bot = bot;

    /// <summary>
    /// Gets the name of this module.
    /// </summary>
    /// <remarks>If not overridden, this defaults to the class's name.</remarks>
    public virtual string Name => GetType().Name;

    /// <summary>
    /// Called when a guild becomes available during initial load or configuration reload.
    /// The implementing class should construct an instance to hold data specific to the corresponding guild for use during runtime.
    /// </summary>
    /// <param name="guildID">Corresponding guild ID for the state data being used. May be useful when reloading.</param>
    /// <param name="config">JSON token holding module configuration specific to this guild.</param>
    /// <returns>
    /// An object instance containing state and/or configuration information for the guild currently being processed.
    /// </returns>
    public abstract Task<object?> CreateGuildStateAsync(ulong guildID, JToken config);

    /// <summary>
    /// Retrieves the state object that corresponds with the given guild.
    /// </summary>
    /// <typeparam name="T">The state instance's type.</typeparam>
    /// <param name="guildId">The guild ID for which to retrieve the state object.</param>
    /// <returns>The state instance cast in the given type, or Default(T) if none exists.</returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if the instance cannot be cast as specified.
    /// </exception>
    [DebuggerStepThrough]
    protected T? GetGuildState<T>(ulong guildId) => Bot._svcGuildState.DoGetStateObj<T>(guildId, GetType());

    /// <summary>
    /// Returns the list of moderators defined in the current guild configuration.
    /// </summary>
    /// <returns>
    /// An <see cref="EntityList"/> with corresponding moderator configuration data.
    /// In case none exists, an empty list will be returned.
    /// </returns>
    protected EntityList GetModerators(ulong guild) => Bot._svcGuildState.DoGetModlist(guild);

    /// <summary>
    /// Emits a log message to the bot console that is associated with the specified guild.
    /// </summary>
    /// <param name="guild">The guld for which this log message is associated with.</param>
    /// <param name="message">The log message to send. Multi-line messages are acceptable.</param>
    protected void Log(SocketGuild guild, string? message) {
        var gname = guild.Name ?? $"Guild ID {guild.Id}";
        Bot._svcLogging.DoLog(false, $"{Name}] [{gname}", message);
    }

    /// <summary>
    /// Emits a log message to the bot console and, optionally, the logging webhook.
    /// </summary>
    /// <param name="message">The log message to send. Multi-line messages are acceptable.</param>
    /// <param name="report">
    /// Specifies if the log message should be sent to the reporting channel.
    /// Only messages of very high importance should use this option.
    /// </param>
    protected void Log(string message, bool report = false) => Bot._svcLogging.DoLog(report, Name, message);
}
