using System.Diagnostics;

namespace RegexBot.Modules.AutoResponder;

/// <summary>
/// Provides the capability to define text responses to pattern-based triggers for fun or informational
/// purposes. Although in essence similar to <see cref="RegexModerator.RegexModerator"/>, it is a better
/// fit for non-moderation use cases and has specific features suitable to that end.
/// </summary>
[RegexbotModule]
internal class AutoResponder : RegexbotModule {
    public AutoResponder(RegexbotClient bot) : base(bot) {
        DiscordClient.MessageReceived += DiscordClient_MessageReceived;
    }

    public override Task<object?> CreateGuildStateAsync(ulong guildID, JToken config) {
        if (config == null) return Task.FromResult<object?>(null);
        var defs = new List<Definition>();

        if (config.Type != JTokenType.Array)
            throw new ModuleLoadException(Name + " configuration must be a JSON array.");

        // TODO better error reporting during this process
        foreach (var def in config.Children<JObject>())
            defs.Add(new Definition(def));

        if (defs.Count == 0) return Task.FromResult<object?>(null);
        Log(DiscordClient.GetGuild(guildID), $"Loaded {defs.Count} definition(s).");
        return Task.FromResult<object?>(defs.AsReadOnly());
    }

    private async Task DiscordClient_MessageReceived(SocketMessage arg) {
        if (!Common.Utilities.IsValidUserMessage(arg, out var ch)) return;

        var definitions = GetGuildState<IEnumerable<Definition>>(ch.Guild.Id);
        if (definitions == null) return; // No configuration in this guild; do no further processing

        var tasks = new List<Task>();
        foreach (var def in definitions) {
            tasks.Add(Task.Run(async () => await ProcessMessageAsync(arg, def)));
        }

        await Task.WhenAll(tasks);
    }

    private async Task ProcessMessageAsync(SocketMessage msg, Definition def) {
        if (!def.Match(msg)) return;

        if (def.Command == null) {
            await msg.Channel.SendMessageAsync(def.GetResponse());
        } else {
            var ch = (SocketGuildChannel)msg.Channel;
            var cmdline = def.Command.Split(new char[] { ' ' }, 2);

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
                    Log(ch.Guild, $"Command execution: Process exited abnormally (with code {p.ExitCode}).");
                }
                using var stdout = p.StandardOutput;
                var result = await stdout.ReadToEndAsync();
                if (!string.IsNullOrWhiteSpace(result)) await msg.Channel.SendMessageAsync(result);
            } else {
                Log(ch.Guild, $"Command execution: Process has not exited in 5 seconds. Killing process.");
                p.Kill();
            }
        }
    }
}
