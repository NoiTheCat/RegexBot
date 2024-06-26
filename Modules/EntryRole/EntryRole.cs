﻿using RegexBot.Common;
using System.Text;

namespace RegexBot.Modules.EntryRole;
/// <summary>
/// Automatically sets a role onto users entering the guild after a predefined amount of time.
/// </summary>
[RegexbotModule]
internal sealed class EntryRole : RegexbotModule, IDisposable {
    readonly Task _workerTask;
    readonly CancellationTokenSource _workerTaskToken;

    public EntryRole(RegexbotClient bot) : base(bot) {
        DiscordClient.GuildMembersDownloaded += DiscordClient_GuildMembersDownloaded;
        DiscordClient.UserJoined += DiscordClient_UserJoined;
        DiscordClient.UserLeft += DiscordClient_UserLeft;

        _workerTaskToken = new CancellationTokenSource();
        _workerTask = Task.Factory.StartNew(RoleApplyWorker, _workerTaskToken.Token,
            TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    void IDisposable.Dispose() {
        _workerTaskToken.Cancel();
        _workerTask.Wait(2000);
        _workerTask.Dispose();
    }

    private Task DiscordClient_GuildMembersDownloaded(SocketGuild arg) {
        var data = GetGuildState<GuildData>(arg.Id);
        if (data == null) return Task.CompletedTask;

        var rolecheck = data.TargetRole.FindRoleIn(arg);
        if (rolecheck == null) {
            Log(arg, "Unable to find target role to be applied. Initial check has been skipped.");
            return Task.CompletedTask;
        }
        foreach (var user in arg.Users.Where(u => !u.Roles.Contains(rolecheck))) {
            data.WaitlistAdd(user.Id);
        }

        return Task.CompletedTask;
    }

    private Task DiscordClient_UserJoined(SocketGuildUser arg) {
        GetGuildState<GuildData>(arg.Guild.Id)?.WaitlistAdd(arg.Id);
        return Task.CompletedTask;
    }

    private Task DiscordClient_UserLeft(SocketGuild guild, SocketUser user) {
        GetGuildState<GuildData>(guild.Id)?.WaitlistRemove(user.Id);
        return Task.CompletedTask;
    }

    public override Task<object?> CreateGuildStateAsync(ulong guildID, JToken? config) {
        if (config == null) return Task.FromResult<object?>(null);

        if (config.Type != JTokenType.Object)
            throw new ModuleLoadException("Configuration is not properly defined.");
        var g = DiscordClient.GetGuild(guildID);

        // Attempt to preserve existing timer list on reload
        var oldconf = GetGuildState<GuildData>(guildID);
        if (oldconf == null) {
            var newconf = new GuildData((JObject)config);
            Log(g, $"Configured for {newconf.WaitTime} seconds.");
            return Task.FromResult<object?>(newconf);
        } else {
            var newconf = new GuildData((JObject)config, oldconf.WaitingList);
            Log(g, $"Reconfigured for {newconf.WaitTime} seconds; keeping {newconf.WaitingList.Count} existing timers.");
            return Task.FromResult<object?>(newconf);
        }
    }

    /// <summary>
    /// Main worker task.
    /// </summary>
    private async Task RoleApplyWorker() {
        while (!_workerTaskToken.IsCancellationRequested) {
            await Task.Delay(5000);

            var subworkers = new List<Task>();
            foreach (var g in DiscordClient.Guilds) {
                subworkers.Add(RoleApplyGuildSubWorker(g));
            }
            Task.WaitAll([.. subworkers]);
        }
    }

    /// <summary>
    /// Guild-specific processing by worker task.
    /// </summary>
    internal async Task RoleApplyGuildSubWorker(SocketGuild g) {
        var gconf = GetGuildState<GuildData>(g.Id);
        if (gconf == null) return;

        // Get list of users to be affected
        ulong[] userIds;
        lock (gconf.WaitingList) {
            if (gconf.WaitingList.Count == 0) return;

            var now = DateTimeOffset.UtcNow;
            var queryIds = from item in gconf.WaitingList
                           where item.Value > now
                           select item.Key;
            userIds = queryIds.ToArray();

            foreach (var item in userIds) gconf.WaitingList.Remove(item);
        }

        var gusers = new List<SocketGuildUser>();
        foreach (var item in userIds) {
            var gu = g.GetUser(item);
            if (gu == null) continue; // silently drop unknown users (is this fine?)
            gusers.Add(gu);
        }
        if (gusers.Count == 0) return;

        // Attempt to get role. 
        var targetRole = gconf.TargetRole.FindRoleIn(g, true);
        if (targetRole == null) {
            ReportFailure(g, "Unable to determine role to be applied. Does it still exist?", gusers);
            return;
        }

        // Apply roles
        try {
            foreach (var item in gusers) {
                if (item.Roles.Contains(targetRole)) continue;
                await item.AddRoleAsync(targetRole);
            }
        } catch (Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Forbidden) {
            ReportFailure(g, "Unable to set role due to a permissions issue.", gusers);
        }
    }

    private void ReportFailure(SocketGuild g, string message, IEnumerable<SocketGuildUser> failedUserList) {
        var failList = new StringBuilder();
        var count = 0;
        foreach (var item in failedUserList) {
            failList.Append($", {item.GetDisplayableUsername()}");
            count++;
            if (count > 5) {
                failList.Append($"and {count} other(s).");
                break;
            }
        }
        failList.Remove(0, 2);
        Log(g, message + " Failed while attempting to set role on the following users: " + failList.ToString());
    }
}
