namespace RegexBot.Modules.ModCommands.Commands;
class ConfReload : CommandConfig {
    protected override string DefaultUsageMsg => null!;

    // No configuration.
    public ConfReload(ModCommands module, JObject config) : base(module, config) { }

    // Usage: (command)
    public override Task Invoke(SocketGuild g, SocketMessage msg) {
        throw new NotImplementedException();
        // bool status = await RegexBot.Config.ReloadServerConfig();
        // string res;
        // if (status) res = ":white_check_mark: Configuration reloaded with no issues. Check the console to verify.";
        // else res = ":x: Reload failed. Check the console.";
        // await msg.Channel.SendMessageAsync(res);
    }
}