using Discord;
using System.Diagnostics;

namespace RegexBot.Modules.ModCommands.Commands;
[DebuggerDisplay("Command definition '{Label}'")]
abstract class CommandConfig(ModCommands module, JObject config) {
    public string Label { get; } = config[nameof(Label)]!.Value<string>()!;
    public string Command { get; } = config[nameof(Command)]!.Value<string>()!;
    protected ModCommands Module { get; } = module;

    public abstract Task Invoke(SocketGuild g, SocketMessage msg);

    protected const string FailDefault = "An unknown error occurred. Notify the bot operator.";
    protected const string TargetNotFound = ":x: **Unable to find the given user.**";

    protected abstract string DefaultUsageMsg { get; }

    /// <summary>
    /// Sends out the default usage message (<see cref="DefaultUsageMsg"/>) within an embed. 
    /// An optional message can be included, for uses such as notifying users of incorrect usage.
    /// </summary>
    /// <param name="target">Target channel for sending the message.</param>
    /// <param name="message">The message to send alongside the default usage message.</param>
    protected async Task SendUsageMessageAsync(ISocketMessageChannel target, string? message = null) {
        if (DefaultUsageMsg == null)
            throw new InvalidOperationException("DefaultUsage was not defined.");

        var usageEmbed = new EmbedBuilder() {
            Title = "Usage",
            Description = DefaultUsageMsg
        };
        await target.SendMessageAsync(message ?? "", embed: usageEmbed.Build());
    }

    internal static readonly char[] separator = [' '];
    /// <summary>
    /// For the given message's content, assumes its message is a command and returns its parameters
    /// as an array of substrings.
    /// </summary>
    /// <param name="msg">The incoming message to process.</param>
    /// <param name="maxParams">The number of parameters to expect.</param>
    /// <returns>A string array with 0 to maxParams - 1 elements.</returns>
    protected static string[] SplitToParams(SocketMessage msg, int maxParams)
        => msg.Content.Split(separator, maxParams, StringSplitOptions.RemoveEmptyEntries);
}
