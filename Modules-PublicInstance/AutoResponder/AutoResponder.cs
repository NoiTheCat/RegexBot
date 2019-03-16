using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kerobot.Modules.AutoResponder
{
    /// <summary>
    /// Provides the capability to define text responses to pattern-based triggers for fun or informational
    /// purposes. Although in essence similar to <see cref="RegexModerator.RegexModerator"/>, it is a better
    /// fit for non-moderation use cases and has specific features suitable to that end.
    /// </summary>
    [KerobotModule]
    class AutoResponder : ModuleBase
    {
        public AutoResponder(Kerobot kb) : base(kb)
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
            if (config == null) return null;
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

        private async Task ProcessMessageAsync(SocketMessage msg, Definition def)
        {
            if (!def.Match(msg)) return;
            await msg.Channel.SendMessageAsync(def.GetResponse());
        }
    }
}
