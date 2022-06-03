namespace RegexBot.Modules.PendingOutRole;

/// <summary>
/// Automatically sets a specified role when a user is no longer in (gets out of) pending status -
/// that is, the user has passed the requirements needed to fully access the guild such as welcome messages, etc.
/// </summary>
[RegexbotModule]
public class PendingOutRole : RegexbotModule {
    public PendingOutRole(RegexbotClient bot) : base(bot) {
        DiscordClient.GuildMembersDownloaded += DiscordClient_GuildMembersDownloaded;
        DiscordClient.GuildMemberUpdated += DiscordClient_GuildMemberUpdated;
    }

    private async Task DiscordClient_GuildMembersDownloaded(SocketGuild arg) {
        var conf = GetGuildState<ModuleConfig>(arg.Id);
        if (conf == null) return;
        var targetRole = conf.Role.FindRoleIn(arg, true);
        if (targetRole == null) {
            Log(arg, "Unable to find role to be applied. Initial check has been skipped.");
            return;
        }

        foreach (var user in arg.Users.Where(u => u.IsPending.HasValue && u.IsPending.Value == false)) {
            if (user.Roles.Contains(targetRole)) continue;
            await user.AddRoleAsync(targetRole);
        }
    }

    private async Task DiscordClient_GuildMemberUpdated(Discord.Cacheable<SocketGuildUser, ulong> previous, SocketGuildUser current) {
        var conf = GetGuildState<ModuleConfig>(current.Guild.Id);
        if (conf == null) return;

        if (!(previous.Value.IsPending.HasValue && current.IsPending.HasValue)) return;
        if (previous.Value.IsPending == true && current.IsPending == false) {
            var r = conf.Role.FindRoleIn(current.Guild, true);
            if (r == null) {
                Log(current.Guild, $"Failed to update role for {current} - was the role renamed or deleted?");
                return;
            }
            await current.AddRoleAsync(r);
        }
    }

    public override Task<object?> CreateGuildStateAsync(ulong guildID, JToken config) {
        if (config == null) return Task.FromResult<object?>(null);
        if (config.Type != JTokenType.Object)
            throw new ModuleLoadException("Configuration for this section is invalid.");
        return Task.FromResult<object?>(new ModuleConfig((JObject)config));
    }
}
