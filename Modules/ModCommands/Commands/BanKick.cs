using RegexBot.Common;
using RegexBot.Data;

namespace RegexBot.Modules.ModCommands.Commands;
// Ban and kick commands are highly similar in implementation, and thus are handled in a single class.
class Ban : BanKick {
    public Ban(ModCommands module, JObject config) : base(module, config, true) {
        if (PurgeDays is > 7 or < 0)
            throw new ModuleLoadException($"The value of '{nameof(PurgeDays)}' must be between 0 and 7.");
    }

    protected override async Task ContinueInvoke(SocketGuild g, SocketMessage msg, string? reason,
                                                 ulong targetId, CachedGuildUser? targetQuery, SocketUser? targetUser) {
        // Ban: Unlike kick, the minimum required is just the target ID
        var result = await Module.Bot.BanAsync(g, msg.Author.ToString(), targetId, PurgeDays, reason, SendNotify);
        if (result.OperationSuccess) {
            if (SuccessMessage != null) {
                // TODO customization
                await msg.Channel.SendMessageAsync($"{SuccessMessage}\n{result.GetResultString(Module.Bot)}");
            } else {
                // TODO custom fail message?
                await msg.Channel.SendMessageAsync(result.GetResultString(Module.Bot));
            }
        } else {
            await msg.Channel.SendMessageAsync(result.GetResultString(Module.Bot));
        }
    }
}

class Kick : BanKick {
    public Kick(ModCommands module, JObject config) : base(module, config, false) { }

    protected override async Task ContinueInvoke(SocketGuild g, SocketMessage msg, string? reason,
                                                 ulong targetId, CachedGuildUser? targetQuery, SocketUser? targetUser) {
        // Kick: Unlike ban, must find the guild user in order to proceed
        if (targetUser == null) {
            await SendUsageMessageAsync(msg.Channel, TargetNotFound);
            return;
        }

        var result = await Module.Bot.KickAsync(g, msg.Author.ToString(), targetId, reason, SendNotify);
        if (result.OperationSuccess) {
            if (SuccessMessage != null) {
                // TODO string replacement, formatting, etc
                await msg.Channel.SendMessageAsync($"{SuccessMessage}\n{result.GetResultString(Module.Bot)}");
            }
        }
        await msg.Channel.SendMessageAsync(result.GetResultString(Module.Bot));
    }
}

abstract class BanKick : CommandConfig {
    protected bool ForceReason { get; }
    protected int PurgeDays { get; }
    protected bool SendNotify { get; }
    protected string? SuccessMessage { get; }

    // Configuration:
    // "ForceReason"     - boolean; Force a reason to be given. Defaults to false.
    // "PurgeDays"       - integer; Number of days of target's post history to delete, if banning.
    //                     Must be between 0-7 inclusive. Defaults to 0.
    // "SendNotify"      - boolean; Whether to send a notification DM explaining the action. Defaults to true.
    // "SuccessMessage"  - string; Message to display on command success. Overrides default.
    protected BanKick(ModCommands module, JObject config, bool ban) : base(module, config) {
        ForceReason = config[nameof(ForceReason)]?.Value<bool>() ?? false;
        PurgeDays = config[nameof(PurgeDays)]?.Value<int>() ?? 0;
        
        SendNotify = config[nameof(SendNotify)]?.Value<bool>() ?? true;
        SuccessMessage = config[nameof(SuccessMessage)]?.Value<string>();

        _usage = $"{Command} `user or user ID` `" + (ForceReason ? "reason" : "[reason]") + "`\n"
            + "Removes the given user from this server"
            + (ban ? " and prevents the user from rejoining" : "") + ". "
            + (ForceReason ? "L" : "Optionally l") + "ogs the reason for the "
            + (ban ? "ban" : "kick") + " to the Audit Log.";
        if (PurgeDays > 0)
            _usage += $"\nAdditionally removes the user's post history for the last {PurgeDays} day(s).";
    }

    private readonly string _usage;
    protected override string DefaultUsageMsg => _usage;

    // Usage: (command) (mention) (reason)
    public override async Task Invoke(SocketGuild g, SocketMessage msg) {
        var line = msg.Content.Split(new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
        string targetstr;
        string? reason;
        if (line.Length < 2) {
            await SendUsageMessageAsync(msg.Channel, null);
            return;
        }
        targetstr = line[1];

        if (line.Length == 3) reason = line[2]; // Reason given - keep it
        else {
            // No reason given
            if (ForceReason) {
                await SendUsageMessageAsync(msg.Channel, ":x: **You must specify a reason.**");
                return;
            }
            reason = null;
        }

        // Gather info to send to specific handlers
        var targetQuery = Module.Bot.EcQueryGuildUser(g.Id, targetstr);
        ulong targetId;
        if (targetQuery != null) targetId = (ulong)targetQuery.UserId;
        else if (ulong.TryParse(targetstr, out var parsed)) targetId = parsed;
        else targetId = default;
        var targetUser = targetId != default ? g.GetUser(targetId) : null;

        if (targetId == default) {
            await SendUsageMessageAsync(msg.Channel, TargetNotFound);
            return;
        }

        if (targetUser != null) {
            // Bot check
            if (targetUser.IsBot) {
                await SendUsageMessageAsync(msg.Channel, ":x: I will not do that. Please remove bots manually.");
                return;
            }
            // Hierarchy check
            if (((SocketGuildUser)msg.Author).Hierarchy <= targetUser.Hierarchy) {
                // Block kick attempts if the invoking user is at or above the target in role hierarchy
                await SendUsageMessageAsync(msg.Channel, ":x: You do not have sufficient permissions to do that.");
                return;
            }
        }

        // Preparation complete, go to specific actions
        try {
            await ContinueInvoke(g, msg, reason, targetId, targetQuery, targetUser);
        } catch (Discord.Net.HttpException ex) {
            if (ex.HttpCode == System.Net.HttpStatusCode.Forbidden) {
                await msg.Channel.SendMessageAsync(":x: " + Messages.ForbiddenGenericError);
            } else if (ex.HttpCode == System.Net.HttpStatusCode.NotFound) {
                await msg.Channel.SendMessageAsync(":x: Encountered a 404 error when processing the request.");
            }
        }
    }

    protected abstract Task ContinueInvoke(SocketGuild g, SocketMessage msg, string? reason,
                                     ulong targetId, CachedGuildUser? targetQuery, SocketUser? targetUser);
}