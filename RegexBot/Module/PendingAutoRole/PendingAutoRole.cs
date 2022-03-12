using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using Noikoio.RegexBot.ConfigItem;
using System.Linq;
using System.Threading.Tasks;

namespace Noikoio.RegexBot.Module.PendingAutoRole {
    /// <summary>
    /// Automatically sets a specified role when a user is no longer in pending status
    /// </summary>
    class PendingAutoRole : BotModule
    {
        // Config:
        // Role: string - Name or ID of the role to apply. Takes EntityName format.
        // WaitTime: number - Amount of time in seconds to wait until applying the role to a new user.
        public PendingAutoRole(DiscordSocketClient client) : base(client)
        {
            client.GuildAvailable += Client_GuildAvailable;
            client.GuildMemberUpdated += Client_GuildMemberUpdated;
        }

        private async Task Client_GuildAvailable(SocketGuild arg) {
            var conf = GetState<ModuleConfig>(arg.Id);
            if (conf == null) return;
            var trole = GetRole(arg);
            if (trole == null) {
                await Log("Unable to enumerate. WAs the role renamed or deleted?");
                return;
            }

            foreach (var user in arg.Users.Where(u => u.IsPending.HasValue && u.IsPending.Value == false)) {
                if (user.Roles.Contains(trole)) continue;
                await user.AddRoleAsync(trole);
            }
        }

        private async Task Client_GuildMemberUpdated(Cacheable<SocketGuildUser, ulong> previous, SocketGuildUser current) {
            var conf = GetState<ModuleConfig>(current.Guild.Id);
            if (conf == null) return;

            if (!(previous.Value.IsPending.HasValue && current.IsPending.HasValue)) return;
            if (previous.Value.IsPending == true && current.IsPending == false) {
                var r = GetRole(current.Guild);
                if (r == null) {
                    await Log($"Failed to update {current} - was the role renamed or deleted?");
                    return;
                }
                await current.AddRoleAsync(r);
            }
        }

        public override Task<object> CreateInstanceState(JToken configSection)
        {
            if (configSection == null) return Task.FromResult<object>(null);
            if (configSection.Type != JTokenType.Object)
            {
                throw new RuleImportException("Configuration for this section is invalid.");
            }
            return Task.FromResult<object>(new ModuleConfig((JObject)configSection));
        }
        
        // can return null
        private SocketRole GetRole(SocketGuild g)
        {
            var conf = GetState<ModuleConfig>(g.Id);
            if (conf == null) return null;
            
            if (conf.Role.Id.HasValue)
            {
                var result = g.GetRole(conf.Role.Id.Value);
                if (result != null) return result;
            }
            else
            {
                foreach (var role in g.Roles)
                    if (string.Equals(conf.Role.Name, role.Name)) return role;
            }
            Log("Unable to find role in " + g.Name).Wait();
            return null;
        }
    }
}
