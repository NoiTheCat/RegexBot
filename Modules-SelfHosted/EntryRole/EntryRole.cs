using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kerobot.Modules.EntryRole
{
    /// <summary>
    /// Automatically sets a role onto users entering the guild.
    /// </summary>
    // TODO add persistent role support, make it an option
    [KerobotModule]
    public class EntryRole : ModuleBase
    {
        readonly Task _workerTask;
        readonly CancellationTokenSource _workerTaskToken; // TODO make use of this when possible

        public EntryRole(Kerobot kb) : base(kb)
        {
            DiscordClient.UserJoined += DiscordClient_UserJoined;
            DiscordClient.UserLeft += DiscordClient_UserLeft;

            _workerTaskToken = new CancellationTokenSource();
            _workerTask = Task.Factory.StartNew(RoleApplyWorker, _workerTaskToken.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
        
        private Task DiscordClient_UserJoined(SocketGuildUser arg)
        {
            GetGuildState<GuildData>(arg.Guild.Id)?.WaitlistAdd(arg.Id);
            return Task.CompletedTask;
        }

        private Task DiscordClient_UserLeft(SocketGuildUser arg)
        {
            GetGuildState<GuildData>(arg.Guild.Id)?.WaitlistRemove(arg.Id);
            return Task.CompletedTask;
        }

        public override Task<object> CreateGuildStateAsync(ulong guildID, JToken config)
        {
            if (config == null) return null;

            if (config.Type != JTokenType.Object)
                throw new ModuleLoadException("Configuration is not properly defined.");

            // Attempt to preserve existing timer list on reload
            var oldconf = GetGuildState<GuildData>(guildID);
            if (oldconf == null) return Task.FromResult<object>(new GuildData((JObject)config));
            else return Task.FromResult<object>(new GuildData((JObject)config, oldconf.WaitingList)); 
        }

        /// <summary>
        /// Main worker task.
        /// </summary>
        private async Task RoleApplyWorker()
        {
            while (!_workerTaskToken.IsCancellationRequested)
            {
                await Task.Delay(5000);

                var subworkers = new List<Task>();
                foreach (var g in DiscordClient.Guilds)
                {
                    subworkers.Add(RoleApplyGuildSubWorker(g));
                }
                Task.WaitAll(subworkers.ToArray());
            }
        }

        /// <summary>
        /// Guild-specific processing by worker task.
        /// </summary>
        internal async Task RoleApplyGuildSubWorker(SocketGuild g)
        {
            var gconf = GetGuildState<GuildData>(g.Id);
            if (gconf == null) return;

            // Get list of users to be affected
            ulong[] userIds;
            lock (gconf.WaitingList)
            {
                if (gconf.WaitingList.Count == 0) return;

                var now = DateTimeOffset.UtcNow;
                var queryIds = from item in gconf.WaitingList
                               where item.Value > now
                               select item.Key;
                userIds = queryIds.ToArray();

                foreach (var item in userIds) gconf.WaitingList.Remove(item);
            }

            var gusers = new List<SocketGuildUser>();
            foreach (var item in userIds)
            {
                var gu = g.GetUser(item);
                if (gu == null) continue; // silently drop unknown users (is this fine?)
                gusers.Add(gu);
            }
            if (gusers.Count == 0) return;

            // Attempt to get role. 
            var targetRole = gconf.TargetRole.FindRoleIn(g, true);
            if (targetRole == null)
            {
                await ReportFailure(g.Id, "Unable to determine role to be applied. Does it still exist?", gusers);
                return;
            }

            // Apply roles
            try
            {
                foreach (var item in gusers)
                {
                    if (item.Roles.Contains(targetRole)) continue;
                    await item.AddRoleAsync(targetRole);
                }
            }
            catch (Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
            {
                await ReportFailure(g.Id, "Unable to set role due to a permissions issue.", gusers);
            }
        }

        private async Task ReportFailure(ulong gid, string message, IEnumerable<SocketGuildUser> failedUserList)
        {
            var failList = new StringBuilder();
            var count = 0;
            foreach (var item in failedUserList) {
                failList.Append($", {item.Username}#{item.Discriminator}");
                count++;
                if (count > 5)
                {
                    failList.Append($"and {count} other(s).");
                    break;
                }
            }
            failList.Remove(0, 2);
            await LogAsync(gid, message + " Failed while attempting to set role on the following users: " + failList.ToString());
        }
    }
}
