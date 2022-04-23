using RegexBot.Common;

namespace RegexBot.Modules.EntryTimeRole;

/// <summary>
/// Contains configuration data as well as per-guild timers for those awaiting role assignment.
/// </summary>
class GuildData {
    /// <summary>
    /// Lock on self.
    /// </summary>
    public Dictionary<ulong, DateTimeOffset> WaitingList { get; }

    /// <summary>
    /// Role to apply.
    /// </summary>
    public EntityName TargetRole { get; }
    /// <summary>
    /// Time to wait until applying the role, in seconds.
    /// </summary>
    public int WaitTime { get; }

    const int WaitTimeMax = 600; // 10 minutes

    public GuildData(JObject conf) : this(conf, new Dictionary<ulong, DateTimeOffset>()) { }

    public GuildData(JObject conf, Dictionary<ulong, DateTimeOffset> _waitingList) {
        WaitingList = _waitingList;

        var cfgRole = conf["Role"]?.Value<string>();
        if (string.IsNullOrWhiteSpace(cfgRole))
            throw new ModuleLoadException("Role value not specified.");
        try {
            TargetRole = new EntityName(cfgRole);
        } catch (ArgumentException) {
            throw new ModuleLoadException("Role config value was not properly specified to be a role.");
        }

        try {
            WaitTime = conf["WaitTime"].Value<int>();
        } catch (NullReferenceException) {
            throw new ModuleLoadException("WaitTime value not specified.");
        } catch (InvalidCastException) {
            throw new ModuleLoadException("WaitTime value must be a number.");
        }

        if (WaitTime > WaitTimeMax) {
            // don't silently correct it
            throw new ModuleLoadException($"WaitTime value may not exceed {WaitTimeMax} seconds.");
        }
        if (WaitTime < 0) {
            throw new ModuleLoadException("WaitTime value may not be negative.");
        }
    }

    public void WaitlistAdd(ulong userId) {
        lock (WaitingList) WaitingList.Add(userId, DateTimeOffset.UtcNow.AddSeconds(WaitTime));
    }

    public void WaitlistRemove(ulong userId) {
        lock (WaitingList) WaitingList.Remove(userId);
    }
}
