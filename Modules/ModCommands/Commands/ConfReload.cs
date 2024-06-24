namespace RegexBot.Modules.ModCommands.Commands;
class ConfReload(ModCommands module, JObject config) : CommandConfig(module, config) {
    protected override string DefaultUsageMsg => null!;

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