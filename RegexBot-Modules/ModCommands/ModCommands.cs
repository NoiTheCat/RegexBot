namespace RegexBot.Modules.ModCommands;
/// <summary>
/// Provides a way to define highly configurable text-based commands for use by moderators within a guild.
/// </summary>
[RegexbotModule]
public class ModCommands : RegexbotModule {
    public ModCommands(RegexbotClient bot) : base(bot) {
        DiscordClient.MessageReceived += Client_MessageReceived;
    }

    private async Task Client_MessageReceived(SocketMessage arg) {
        if (Common.Utilities.IsValidUserMessage(arg, out var channel)) {
            var cfg = GetGuildState<ModuleConfig>(channel.Guild.Id);
            if (cfg != null) await CommandCheckInvoke(cfg, arg);
        }
    }

    public override Task<object?> CreateGuildStateAsync(ulong guildID, JToken config) {
        if (config == null) return Task.FromResult<object?>(null);

        var conf = new ModuleConfig(this, config);
        if (conf.Commands.Count > 0) {
            Log($"{conf.Commands.Count} commands loaded.");
            return Task.FromResult<object?>(conf);
        } else {
            Log("'{nameof(ModLogs)}' is defined, but no command configuration exists.");
            return Task.FromResult<object?>(null);
        }
    }

    private async Task CommandCheckInvoke(ModuleConfig cfg, SocketMessage arg) {
        SocketGuild g = ((SocketGuildUser)arg.Author).Guild;

        if (!GetModerators(g.Id).IsListMatch(arg, true)) return; // Mods only
        // Disregard if the message contains a newline character
        if (arg.Content.Contains('\n')) return; // TODO remove?

        // Check for and invoke command
        string cmdchk;
        var space = arg.Content.IndexOf(' ');
        if (space != -1) cmdchk = arg.Content[..space];
        else cmdchk = arg.Content;
        if (cfg.Commands.TryGetValue(cmdchk, out var c)) {
            try {
                await c.Invoke(g, arg);
                // TODO Custom post-invocation log messages? Not by the user, but by the command.
                Log($"[{g.Name}] {c.Command} invoked by {arg.Author} in #{arg.Channel.Name}.");
            } catch (Exception ex) {
                Log($"Unhandled exception while processing '{c.Label}':\n" + ex.ToString());
                await arg.Channel.SendMessageAsync($":x: An error occurred during processing ({ex.GetType().FullName}). " +
                    "Check the console for details.");
            }
        }
    }
}
