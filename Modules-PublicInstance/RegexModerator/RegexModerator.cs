using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kerobot.Modules.RegexModerator
{
    /// <summary>
    /// The 'star' feature of Kerobot. Users define pattern-based rules with other constraints.
    /// When triggered, each rule executes one or more different actions.
    /// </summary>
    [KerobotModule]
    public class RegexModerator : ModuleBase
    {
        public RegexModerator(Kerobot kb) : base(kb)
        {
            DiscordClient.MessageReceived += DiscordClient_MessageReceived;
            DiscordClient.MessageUpdated += DiscordClient_MessageUpdated;
        }

        public override Task<object> CreateGuildStateAsync(ulong guildID, JToken config)
        {
            if (config == null) return Task.FromResult<object>(null);
            var defs = new List<ConfDefinition>();

            foreach (var def in config.Children<JObject>())
                defs.Add(new ConfDefinition(this, def, guildID));

            if (defs.Count == 0) return Task.FromResult<object>(null);
            return Task.FromResult<object>(defs.AsReadOnly());
        }

        private Task DiscordClient_MessageUpdated(Discord.Cacheable<Discord.IMessage, ulong> arg1,
            SocketMessage arg2, ISocketMessageChannel arg3)
            => ReceiveIncomingMessage(arg2);
        private Task DiscordClient_MessageReceived(SocketMessage arg) => ReceiveIncomingMessage(arg);

        /// <summary>
        /// Does initial message checking before further processing.
        /// </summary>
        private async Task ReceiveIncomingMessage(SocketMessage msg)
        {
            // Ignore non-guild channels
            if (!(msg.Channel is SocketGuildChannel ch)) return;

            // Get config?
            var defs = GetGuildState<IEnumerable<ConfDefinition>>(ch.Guild.Id);
            if (defs == null) return;

            // Send further processing to thread pool.
            // Match checking is a CPU-intensive task, thus very little checking is done here.
            foreach (var item in defs)
            {
                // Need to check sender's moderator status here. Definition can't access mod list.
                var isMod = GetModerators(ch.Guild.Id).IsListMatch(msg, true);

                var match = item.IsMatch(msg, isMod);
                await Task.Run(async () => await ProcessMessage(item, msg, isMod));
            }
        }

        /// <summary>
        /// Does further message checking and response execution.
        /// Invocations of this method are meant to be on the thread pool.
        /// </summary>
        private async Task ProcessMessage(ConfDefinition def, SocketMessage msg, bool isMod)
        {
            // Reminder: IsMatch handles matching execution time
            if (!def.IsMatch(msg, isMod)) return;

            // TODO logging options for match result; handle here?

            var executor = new ResponseExecutor(def, Kerobot);
            await executor.Execute(msg);
        }
    }
}
