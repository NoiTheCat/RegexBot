using RegexBot.Common;
using System.Collections.ObjectModel;

namespace RegexBot.Modules.VoiceRoleSync;
class ModuleConfig {
    /// <summary>
    /// Key = voice channel ID, Value = role ID.
    /// </summary>
    private readonly ReadOnlyDictionary<ulong, ulong> _values;

    public int Count { get => _values.Count; }

    public ModuleConfig(JObject config, SocketGuild g) {
        // Configuration: Object with properties.
        // Property name is a role entity name
        // Value is a string or array of voice channel IDs.
        var values = new Dictionary<ulong, ulong>();

        foreach (var item in config.Properties()) {
            EntityName name;
            try {
                name = new EntityName(item.Name, EntityType.Role);
            } catch (FormatException) {
                throw new ModuleLoadException($"'{item.Name}' is not specified as a role.");
            }
            var role = name.FindRoleIn(g);
            if (role == null) throw new ModuleLoadException($"Unable to find role '{name}'.");

            var channels = Utilities.LoadStringOrStringArray(item.Value);
            if (channels.Count == 0) throw new ModuleLoadException($"One or more channels must be defined under '{name}'.");
            foreach (var id in channels) {
                if (!ulong.TryParse(id, out var channelId)) throw new ModuleLoadException("Voice channel IDs must be numeric.");
                if (values.ContainsKey(channelId)) throw new ModuleLoadException($"'{channelId}' cannot be specified more than once.");
                values.Add(channelId, role.Id);
            }
        }
        _values = new(values);
    }

    public SocketRole? GetAssociatedRoleFor(SocketVoiceChannel voiceChannel) {
        if (voiceChannel == null) return null;
        if (_values.TryGetValue(voiceChannel.Id, out var roleId)) return voiceChannel.Guild.GetRole(roleId);
        return null;
    }

    public IEnumerable<SocketRole> GetTrackedRoles(SocketGuild guild) {
        var roles = _values.Select(v => v.Value).Distinct();
        foreach (var id in roles) {
            var r = guild.GetRole(id);
            if (r != null) yield return r;
        }
    }
}
