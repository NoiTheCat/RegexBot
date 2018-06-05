using System.Collections.Generic;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Kerobot.Modules
{
    [KerobotModule]
    class TestMod : ModuleBase
    {
        public TestMod(Kerobot kb) : base(kb)
        {
            kb.DiscordClient.MessageReceived += DiscordClient_MessageReceived;
        }

        private async Task DiscordClient_MessageReceived(SocketMessage arg)
        {
            if (arg.Content.ToLower() == ".test")
            {
                await arg.Channel.SendMessageAsync("I respond to your test.");
            }
        }

        public override Task<object> CreateGuildStateAsync(JToken config)
            => Task.FromResult<object>(new Dictionary<ulong, string>());
    }
}
