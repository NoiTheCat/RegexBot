using RegexBot.Common;

namespace RegexBot.Modules.ModCommands.Commands;
class Timeout : CommandConfig {
    protected bool ForceReason { get; }
    protected bool SendNotify { get; }
    protected string? SuccessMessage { get; }

    // Configuration:
    // "ForceReason"     - boolean; Force a reason to be given. Defaults to false.
    // "SendNotify"      - boolean; Whether to send a notification DM explaining the action. Defaults to true.
    // "SuccessMessage"  - string; Additional message to display on command success.
    // TODO future configuration ideas: max timeout, min timeout, default timeout span...
    public Timeout(ModCommands module, JObject config) : base(module, config) {
        ForceReason = config[nameof(ForceReason)]?.Value<bool>() ?? false;
        SendNotify = config[nameof(SendNotify)]?.Value<bool>() ?? true;
        SuccessMessage = config[nameof(SuccessMessage)]?.Value<string>();

        _usage = $"{Command} `user ID or tag` `time in minutes` `" + (ForceReason ? "reason" : "[reason]") + "`\n"
            + "Issues a timeout to the given user, preventing them from participating in the server for a set amount of time. "
            + (ForceReason ? "L" : "Optionally l") + "ogs the reason for the timeout to the Audit Log.";
    }

    private readonly string _usage;
    protected override string DefaultUsageMsg => _usage;

    // Usage: (command) (user) (duration) (reason)
    public override async Task Invoke(SocketGuild g, SocketMessage msg) {
        var line = msg.Content.Split(new char[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
        string targetstr;
        string? reason;
        if (line.Length < 3) {
            await SendUsageMessageAsync(msg.Channel, null);
            return;
        }
        targetstr = line[1];

        if (line.Length == 4) reason = line[3]; // Reason given - keep it
        else {
            // No reason given
            if (ForceReason) {
                await SendUsageMessageAsync(msg.Channel, ":x: **You must specify a reason.**");
                return;
            }
            reason = null;
        }

        if (!int.TryParse(line[2], out var timeParam)) {
            await SendUsageMessageAsync(msg.Channel, ":x: You must specify a duration for the timeout (in minutes).");
            return;
        }

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

        var result = await Module.Bot.SetTimeoutAsync(g, msg.Author.AsEntityNameString(), targetUser,
                                                      TimeSpan.FromMinutes(timeParam), reason, SendNotify);
        if (result.Success && SuccessMessage != null) {
            var success = Utilities.ProcessTextTokens(SuccessMessage, msg);
            await msg.Channel.SendMessageAsync($"{success}\n{result.ToResultString()}");
        } else {
            await msg.Channel.SendMessageAsync(result.ToResultString());
        }
    }
}