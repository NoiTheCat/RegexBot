using RegexBot.Common;

namespace RegexBot.Modules.ModCommands.Commands;
class Untimeout : CommandConfig {
    private readonly string _usage;

    protected override string DefaultUsageMsg => _usage;

    // No configuration.
    // TODO bring in some options from BanKick. Particularly custom success msg.
    // TODO when ModLogs fully implemented, add a reason?
    public Untimeout(ModCommands module, JObject config) : base(module, config) {
        _usage = $"{Command} `user or user ID`\n"
            + "Unsets a timeout from a given user.";
    }

    // Usage: (command) (user query)
    public override async Task Invoke(SocketGuild g, SocketMessage msg) {
        var line = SplitToParams(msg, 3);
        string targetstr;
        if (line.Length < 2) {
            await SendUsageMessageAsync(msg.Channel, null);
            return;
        }
        targetstr = line[1];

        SocketGuildUser? target = null;
        var query = Module.Bot.EcQueryUser(targetstr);
        if (query != null) {
            target = g.GetUser((ulong)query.UserId);
        }
        if (target == null) {
            await SendUsageMessageAsync(msg.Channel, TargetNotFound);
            return;
        }

        // Check if timed out, respond accordingly
        if (target.TimedOutUntil.HasValue && target.TimedOutUntil.Value <= DateTimeOffset.UtcNow) {
            await msg.Channel.SendMessageAsync($":x: **{target}** is not timed out.");
            return;
        }

        // Do the action
        try {
            await target.RemoveTimeOutAsync();
        } catch (Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Forbidden) {
            const string FailPrefix = ":x: **Could not remove timeout:** ";
            await msg.Channel.SendMessageAsync(FailPrefix + Messages.ForbiddenGenericError);
        }
    }
}