using RegexBot.Common;

namespace RegexBot.Modules.ModCommands.Commands;
class Say : CommandConfig {
    private readonly string _usage;
    protected override string DefaultUsageMsg => _usage;

    // No configuration at the moment.
    // TODO: Whitelist/blacklist - to limit which channels it can "say" into
    public Say(ModCommands module, JObject config) : base(module, config) {
        _usage = $"{Command} `channel` `message`\n"
            + "Displays the given message exactly as specified to the given channel.";
    }

    public override async Task Invoke(SocketGuild g, SocketMessage msg) {
        var line = msg.Content.Split(new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
        if (line.Length <= 1) {
            await SendUsageMessageAsync(msg.Channel, ":x: You must specify a channel.");
            return;
        }
        if (line.Length <= 2 || string.IsNullOrWhiteSpace(line[2])) {
            await SendUsageMessageAsync(msg.Channel, ":x: You must specify a message.");
            return;
        }

        var getCh = Utilities.ChannelMention.Match(line[1]);
        if (!getCh.Success) {
            await SendUsageMessageAsync(msg.Channel, ":x: Unable to find given channel.");
            return;
        }
        var ch = g.GetTextChannel(ulong.Parse(getCh.Groups["snowflake"].Value));
        await ch.SendMessageAsync(line[2]);
    }
}
