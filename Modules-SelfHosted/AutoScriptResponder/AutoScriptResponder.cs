using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Kerobot.Modules.AutoScriptResponder
{
    /// <summary>
    /// Meant to be highly identical to AutoResponder, save for its differentiating feature.
    /// This may not be the best approach to it, but do try and copy any relevant changes from one into
    /// the other whenever they occur.
    /// The feature in question: It executes external scripts and replies with their output.
    /// </summary>
    [KerobotModule]
    class AutoScriptResponder : ModuleBase
    {
        public AutoScriptResponder(Kerobot kb) : base(kb)
        {
            DiscordClient.MessageReceived += DiscordClient_MessageReceived;
        }

        private async Task DiscordClient_MessageReceived(SocketMessage arg)
        {
            if (!(arg.Channel is SocketGuildChannel ch)) return;
            if (arg.Author.IsBot || arg.Author.IsWebhook) return;

            var definitions = GetGuildState<IEnumerable<Definition>>(ch.Guild.Id);
            if (definitions == null) return; // No configuration in this guild; do no further processing

            var tasks = new List<Task>();
            foreach (var def in definitions)
            {
                tasks.Add(Task.Run(async () => await ProcessMessageAsync(arg, def)));
            }

            await Task.WhenAll(tasks);
        }

        public override Task<object> CreateGuildStateAsync(ulong guild, JToken config)
        {
            // Guild state is a read-only IEnumerable<Definition>
            if (config == null) return Task.FromResult<object>(null);
            var guildDefs = new List<Definition>();
            foreach (var defconf in config.Children<JProperty>())
            {
                // Getting all JProperties in the section.
                // Validation of data is left to the Definition constructor. ModuleLoadException thrown here:
                var def = new Definition(defconf);
                guildDefs.Add(def);
                // TODO global options
            }

            return Task.FromResult<object>(guildDefs.AsReadOnly());
        }

        // ASR edit: this whole thing.
        private async Task ProcessMessageAsync(SocketMessage msg, Definition def)
        {
            if (!def.Match(msg)) return;

            var ch = (SocketGuildChannel)msg.Channel;
            string[] cmdline = def.Command.Split(new char[] { ' ' }, 2);

            var ps = new ProcessStartInfo()
            {
                FileName = cmdline[0],
                Arguments = (cmdline.Length == 2 ? cmdline[1] : ""),
                UseShellExecute = false, // ???
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            using (Process p = Process.Start(ps))
            {
                p.WaitForExit(5000); // waiting 5 seconds at most
                if (p.HasExited)
                {
                    if (p.ExitCode != 0)
                    {
                        await LogAsync(ch.Guild.Id, $"'{def.Label}': Process exited abnormally (with code {p.ExitCode}).");
                    }
                    using (var stdout = p.StandardOutput)
                    {
                        var result = await stdout.ReadToEndAsync();
                        if (!string.IsNullOrWhiteSpace(result)) await msg.Channel.SendMessageAsync(result);
                    }
                }
                else
                {
                    await LogAsync(ch.Guild.Id, $"'{def.Label}': Process has not exited in 5 seconds. Killing process.");
                    p.Kill();
                }
            }
        }
    }
}
