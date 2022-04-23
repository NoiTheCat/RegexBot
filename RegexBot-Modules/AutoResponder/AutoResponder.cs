using System.Diagnostics;

namespace RegexBot.Modules.AutoResponder;

/// <summary>
/// Provides the capability to define text responses to pattern-based triggers for fun or informational
/// purposes. Although in essence similar to <see cref="RegexModerator.RegexModerator"/>, it is a better
/// fit for non-moderation use cases and has specific features suitable to that end.
/// </summary>
[RegexbotModule]
public class AutoResponder : RegexbotModule {
    public AutoResponder(RegexbotClient bot) : base(bot) {
        DiscordClient.MessageReceived += DiscordClient_MessageReceived;
    }

    private async Task DiscordClient_MessageReceived(SocketMessage arg) {
        if (arg.Channel is not SocketGuildChannel ch) return;
        if (arg.Author.IsBot || arg.Author.IsWebhook) return;

        var definitions = GetGuildState<IEnumerable<Definition>>(ch.Guild.Id);
        if (definitions == null) return; // No configuration in this guild; do no further processing

        var tasks = new List<Task>();
        foreach (var def in definitions) {
            tasks.Add(Task.Run(async () => await ProcessMessageAsync(arg, def)));
        }

        await Task.WhenAll(tasks);
    }

    public override Task<object?> CreateGuildStateAsync(ulong guild, JToken config) {
        // Guild state is a read-only IEnumerable<Definition>
        if (config == null) return Task.FromResult<object?>(null);
        var guildDefs = new List<Definition>();
        foreach (var defconf in config.Children<JProperty>()) {
            // Validation of data is left to the Definition constructor
            var def = new Definition(defconf); // ModuleLoadException may be thrown here
            guildDefs.Add(def);
            // TODO global options
        }

        return Task.FromResult<object?>(guildDefs.AsReadOnly());
    }

    private async Task ProcessMessageAsync(SocketMessage msg, Definition def) {
        if (!def.Match(msg)) return;

        if (def.Command == null) {
            await msg.Channel.SendMessageAsync(def.GetResponse());
        } else {
            var ch = (SocketGuildChannel)msg.Channel;
            string[] cmdline = def.Command.Split(new char[] { ' ' }, 2);

            var ps = new ProcessStartInfo() {
                FileName = cmdline[0],
                Arguments = (cmdline.Length == 2 ? cmdline[1] : ""),
                UseShellExecute = false, // ???
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            using var p = Process.Start(ps)!;

            p.WaitForExit(5000); // waiting 5 seconds at most
            if (p.HasExited) {
                if (p.ExitCode != 0) {
                    PLog($"Command execution in {ch.Guild.Id}: Process exited abnormally (with code {p.ExitCode}).");
                }
                using var stdout = p.StandardOutput;
                var result = await stdout.ReadToEndAsync();
                if (!string.IsNullOrWhiteSpace(result)) await msg.Channel.SendMessageAsync(result);
            } else {
                PLog($"Command execution in {ch.Guild.Id}: Process has not exited in 5 seconds. Killing process.");
                p.Kill();
            }
        }
    }
}
