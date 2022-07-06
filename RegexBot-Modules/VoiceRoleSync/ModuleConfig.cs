using System.Collections.ObjectModel;

namespace RegexBot.Modules.VoiceRoleSync;
/// <summary>
/// Dictionary wrapper. Key = voice channel ID, Value = role.
/// </summary>
class ModuleConfig {
    private readonly ReadOnlyDictionary<ulong, ulong> _values;

    public ModuleConfig(JObject config) {
        // Configuration format is expected to be an object that contains other objects.
        // The objects themselves should have their name be the voice channel,
        // and the value be the role to be applied.

        // TODO Make it accept names; currently only accepts ulongs

        var values = new Dictionary<ulong, ulong>();

        foreach (var item in config.Properties()) {
            if (!ulong.TryParse(item.Name, out var voice)) throw new ModuleLoadException($"{item.Name} is not a voice channel ID.");
            var valstr = item.Value.Value<string>();
            if (!ulong.TryParse(valstr, out var role)) throw new ModuleLoadException($"{valstr} is not a role ID.");

            values[voice] = role;
        }

        _values = new ReadOnlyDictionary<ulong, ulong>(values);
    }

    public SocketRole? GetAssociatedRoleFor(SocketVoiceChannel voiceChannel) {
        if (voiceChannel == null) return null;
        if (_values.TryGetValue(voiceChannel.Id, out var roleId)) return voiceChannel.Guild.GetRole(roleId);
        return null;
    }

    public IEnumerable<SocketRole> GetTrackedRoles(SocketGuild guild) {
        foreach (var pair in _values) {
            var r = guild.GetRole(pair.Value);
            if (r != null) yield return r;
        }
    }
}
