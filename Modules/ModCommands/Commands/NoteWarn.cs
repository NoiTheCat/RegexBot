using RegexBot.Common;

namespace RegexBot.Modules.ModCommands.Commands;
// Note and Warn commands are highly similar in implementnation, and thus are handled in a single class.
class Note(ModCommands module, JObject config) : NoteWarn(module, config) {
    protected override string DefaultUsageMsg => string.Format(_usageHeader, Command)
        + "Appends a note to the moderation log for the given user.";
    protected override async Task ContinueInvoke(SocketGuild g, SocketMessage msg, string logMessage, SocketUser targetUser) {
        var result = await Module.Bot.AddUserNoteAsync(g, targetUser.Id, msg.Author.AsEntityNameString(), logMessage);
        await msg.Channel.SendMessageAsync($":white_check_mark: Note \\#{result.LogId} logged for {targetUser}.");
    }
}

class Warn(ModCommands module, JObject config) : NoteWarn(module, config) {
    protected override string DefaultUsageMsg => string.Format(_usageHeader, Command)
        + "Issues a warning to the given user, logging the instance to this bot's moderation log "
        + "and notifying the offending user over DM of the issued warning.";

    protected override async Task ContinueInvoke(SocketGuild g, SocketMessage msg, string logMessage, SocketUser targetUser) {
        // Won't warn a bot
        if (targetUser.IsBot) {
            await SendUsageMessageAsync(msg.Channel, ":x: I don't want to do that. If you must, please warn bots manually.");
            return;
        }

        var (_, result) = await Module.Bot.AddUserWarnAsync(g, targetUser.Id, msg.Author.AsEntityNameString(), logMessage);
        await msg.Channel.SendMessageAsync(result.GetResultString());
    }
}

abstract class NoteWarn : CommandConfig {
    protected string? SuccessMessage { get; }

    protected const string _usageHeader = "{0}  `user ID or tag` `message`\n";

    // Configuration:
    // "SuccessMessage" - string; Additional message to display on command success.
    protected NoteWarn(ModCommands module, JObject config) : base(module, config) {
        SuccessMessage = config[nameof(SuccessMessage)]?.Value<string>();
    }

    // Usage: (command) (user) (message)
    public override async Task Invoke(SocketGuild g, SocketMessage msg) {
        var line = SplitToParams(msg, 3);
        if (line.Length != 3) {
            await SendUsageMessageAsync(msg.Channel, ":x: Not all required parameters were specified.");
            return;
        }
        var targetstr = line[1];
        var logMessage = line[2];

        // Get target user. Required to find for our purposes.
        var targetQuery = Module.Bot.EcQueryGuildUser(g.Id, targetstr);
        ulong targetId;
        if (targetQuery != null) targetId = (ulong)targetQuery.UserId;
        else if (ulong.TryParse(targetstr, out var parsed)) targetId = parsed;
        else {
            await SendUsageMessageAsync(msg.Channel, TargetNotFound);
            return;
        }
        var targetUser = g.GetUser(targetId);

        // Go to specific action
        if (targetUser == null) {
            await msg.Channel.SendMessageAsync(":x: Unable to find the specified user.");
        } else {
            await ContinueInvoke(g, msg, logMessage, targetUser);
        }
    }

    protected abstract Task ContinueInvoke(SocketGuild g, SocketMessage msg, string logMessage, SocketUser targetUser);
}