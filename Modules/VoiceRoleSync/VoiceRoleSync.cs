namespace RegexBot.Modules.VoiceRoleSync;
/// <summary>
/// Synchronizes a user's state in a voice channel with a role.
/// In other words: applies a role to a user entering a voice channel. Removes the role when exiting.
/// </summary>
[RegexbotModule]
internal class VoiceRoleSync : RegexbotModule {
    // TODO wishlist? specify multiple definitions - multiple channels associated with multiple roles.

    public VoiceRoleSync(RegexbotClient bot) : base(bot) {
        DiscordClient.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;
    }

    private async Task Client_UserVoiceStateUpdated(SocketUser argUser, SocketVoiceState before, SocketVoiceState after) {
        // Gather data.
        if (argUser is not SocketGuildUser user) return; // not a guild user
        var settings = GetGuildState<ModuleConfig>(user.Guild.Id);
        if (settings == null) return; // not enabled here

        async Task RemoveAllAssociatedRoles()
            => await user.RemoveRolesAsync(settings.GetTrackedRoles(user.Guild).Intersect(user.Roles));

        if (after.VoiceChannel == null) {
            // Not in any voice channel. Remove all roles being tracked by this instance. Clear.
            await RemoveAllAssociatedRoles();
        } else {
            // In a voice channel, and...
            if (after.IsDeafened || after.IsSelfDeafened) {
                // Is defeaned, which is like not being in a voice channel for our purposes. Clear.
                await RemoveAllAssociatedRoles();
            } else {
                var targetRole = settings.GetAssociatedRoleFor(after.VoiceChannel);
                if (targetRole == null) {
                    // In an untracked voice channel. Clear.
                    await RemoveAllAssociatedRoles();
                } else {
                    // In a tracked voice channel: Clear all except target, add target if needed.
                    await user.RemoveRolesAsync(settings.GetTrackedRoles(user.Guild)
                        .Where(role => role.Id != targetRole.Id)
                        .Intersect(user.Roles));
                    if (!user.Roles.Contains(targetRole)) await user.AddRoleAsync(targetRole);
                }
            }
        }
    }

    public override Task<object?> CreateGuildStateAsync(ulong guildID, JToken config) {
        if (config == null) return Task.FromResult<object?>(null);
        if (config.Type != JTokenType.Object)
            throw new ModuleLoadException("Configuration for this section is invalid.");

        var newconf = new ModuleConfig((JObject)config);
        Log(DiscordClient.GetGuild(guildID), $"Configured with {newconf.Count} pairing(s).");
        return Task.FromResult<object?>(newconf);
    }
}
