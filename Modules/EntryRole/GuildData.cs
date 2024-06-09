using RegexBot.Common;

namespace RegexBot.Modules.EntryRole;
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

    public GuildData(JObject conf) : this(conf, []) { }

    public GuildData(JObject conf, Dictionary<ulong, DateTimeOffset> _waitingList) {
        WaitingList = _waitingList;

        try {
            TargetRole = new EntityName(conf["Role"]?.Value<string>()!, EntityType.Role);
        } catch (Exception) {
            throw new ModuleLoadException("'Role' was not properly specified.");
        }

        try {
            WaitTime = conf[nameof(WaitTime)]!.Value<int>();
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
        lock (WaitingList) {
            if (!WaitingList.ContainsKey(userId)) WaitingList.Add(userId, DateTimeOffset.UtcNow.AddSeconds(WaitTime));
        }
    }

    public void WaitlistRemove(ulong userId) {
        lock (WaitingList) WaitingList.Remove(userId);
    }
}
