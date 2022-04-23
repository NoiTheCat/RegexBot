namespace RegexBot.Modules.PendingOutRole;

/// <summary>
/// Automatically sets a specified role when a user is no longer in (gets out of) pending status -
/// that is, the user has passed the requirements needed to fully access the guild such as welcome messages, etc.
/// </summary>
[RegexbotModule]
public class PendingOutRole : RegexbotModule {
    public PendingOutRole(RegexbotClient bot) : base(bot) {
        DiscordClient.GuildAvailable += DiscordClient_GuildAvailable;
        DiscordClient.GuildMemberUpdated += DiscordClient_GuildMemberUpdated;
    }

    private async Task DiscordClient_GuildAvailable(SocketGuild arg) {
        var conf = GetGuildState<ModuleConfig>(arg.Id);
        if (conf == null) return;
        var trole = GetRole(arg);
        if (trole == null) {
            Log(arg.Id, "Unable to find target role to be applied. Was it renamed or deleted?");
            return;
        }

        foreach (var user in arg.Users.Where(u => u.IsPending.HasValue && u.IsPending.Value == false)) {
            if (user.Roles.Contains(trole)) continue;
            await user.AddRoleAsync(trole);
        }
    }

    private async Task DiscordClient_GuildMemberUpdated(Discord.Cacheable<SocketGuildUser, ulong> previous, SocketGuildUser current) {
        var conf = GetGuildState<ModuleConfig>(current.Guild.Id);
        if (conf == null) return;

        if (!(previous.Value.IsPending.HasValue && current.IsPending.HasValue)) return;
        if (previous.Value.IsPending == true && current.IsPending == false) {
            var r = GetRole(current.Guild);
            if (r == null) {
                Log(current.Guild.Id, $"Failed to update {current} - was the role renamed or deleted?");
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

    private SocketRole? GetRole(SocketGuild g) {
        var conf = GetGuildState<ModuleConfig>(g.Id);
        if (conf == null) return null;

        if (conf.Role.Id.HasValue) {
            var result = g.GetRole(conf.Role.Id.Value);
            if (result != null) return result;
        } else {
            foreach (var role in g.Roles) {
                if (string.Equals(conf.Role.Name, role.Name, StringComparison.OrdinalIgnoreCase)) return role;
            }
        }
        Log(g.Id, "Unable to find role in " + g.Name);
        return null;
    }
}
