using Discord;
using System.Diagnostics;

namespace RegexBot.Modules.ModCommands.Commands;
[DebuggerDisplay("Command definition '{Label}'")]
abstract class CommandConfig {
    public string Label { get; }
    public string Command { get; }
    protected ModCommands Module { get; }

    protected CommandConfig(ModCommands module, JObject config) {
        Module = module;
        Label = config[nameof(Label)]!.Value<string>()!;
        Command = config[nameof(Command)]!.Value<string>()!;
    }

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
}
