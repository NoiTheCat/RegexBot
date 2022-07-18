namespace RegexBot.Modules.ModCommands.Commands;
class Unban : CommandConfig {
    private readonly string _usage;

    protected override string DefaultUsageMsg => _usage;

    // No configuration.
    // TODO bring in some options from BanKick. Particularly custom success msg.
    // TODO when ModLogs fully implemented, add a reason?
    public Unban(ModCommands module, JObject config) : base(module, config) {
        _usage = $"{Command} `user or user ID`\n"
            + "Unbans the given user, allowing them to rejoin the server.";
    }

    // Usage: (command) (user query)
    public override async Task Invoke(SocketGuild g, SocketMessage msg) {
        var line = msg.Content.Split(new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
        string targetstr;
        if (line.Length < 2) {
            await SendUsageMessageAsync(msg.Channel, null);
            return;
        }
        targetstr = line[1];

        ulong targetId;
        string targetDisplay;
        var query = Module.Bot.EcQueryUser(targetstr);
        if (query != null) {
            targetId = (ulong)query.UserId;
            targetDisplay = $"{query.Username}#{query.Discriminator}";
        } else {
            if (!ulong.TryParse(targetstr, out targetId)) {
                await SendUsageMessageAsync(msg.Channel, TargetNotFound);
                return;
            }
            targetDisplay = $"with ID {targetId}";
        }

        // Do the action
        try {
            await g.RemoveBanAsync(targetId);
            await msg.Channel.SendMessageAsync($":white_check_mark: Unbanned user **{targetDisplay}**.");
        } catch (Discord.Net.HttpException ex) {
            const string FailPrefix = ":x: **Could not unban:** ";
            if (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                await msg.Channel.SendMessageAsync(FailPrefix + Strings.ForbiddenGenericError);
            else if (ex.HttpCode == System.Net.HttpStatusCode.NotFound)
                await msg.Channel.SendMessageAsync(FailPrefix + "The specified user does not exist or is not in the ban list.");
            else throw;
        }
    }
}