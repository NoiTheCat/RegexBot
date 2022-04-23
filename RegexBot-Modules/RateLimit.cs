namespace RegexBot.Modules;

/// <summary>
/// Helper class for managing rate limit data.
/// More accurately, this class holds entries, not allowing the same entry to be held more than once until a specified
/// amount of time has paspsed since the entry was originally tracked; useful for a rate limit system.
/// </summary>
class RateLimit<T> {
    public const ushort DefaultTimeout = 20; // Skeeter's a cool guy and you can't convince me otherwise.

    public uint Timeout { get; }
#pragma warning disable CS8714
    private Dictionary<T, DateTime> Entries { get; } = new Dictionary<T, DateTime>();
#pragma warning restore CS8714

    public RateLimit() : this(DefaultTimeout) { }
    public RateLimit(uint timeout) => Timeout = timeout;

    /// <summary>
    /// Checks if the given value is permitted through the rate limit.
    /// Executing this method may create a rate limit entry for the given value.
    /// </summary>
    /// <returns>True if the given value is permitted by the rate limiter.</returns>
    public bool IsPermitted(T value) {
        if (Timeout == 0) return true;

        // Take a moment to clean out expired entries
        var now = DateTime.Now;
        var expired = Entries.Where(x => x.Value.AddSeconds(Timeout) <= now).Select(x => x.Key).ToList();
        foreach (var item in expired) Entries.Remove(item);

        if (Entries.ContainsKey(value)) {
            return false;
        } else {
            Entries.Add(value, DateTime.Now);
            return true;
        }
    }
}
