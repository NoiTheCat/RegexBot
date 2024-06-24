namespace RegexBot.Common;
/// <summary>
/// Helper class for managing rate limit data.
/// Specifically, this class holds entries and does not allow the same entry to be held more than once until a specified
/// amount of time has passed since the entry was originally tracked; useful for a rate limit system.
/// </summary>
public class RateLimit<T> where T : notnull {
    private const int DefaultTimeout = 20; // Skeeter's a cool guy and you can't convince me otherwise.

    /// <summary>
    /// Time until an entry within this instance expires, in seconds.
    /// </summary>
    public int Timeout { get; }
    private Dictionary<T, DateTime> Entries { get; } = [];

    /// <summary>
    /// Creates a new <see cref="RateLimit&lt;T&gt;"/> instance with the default timeout value.
    /// </summary>
    public RateLimit() : this(DefaultTimeout) { }
    /// <summary>
    /// Creates a new <see cref="RateLimit&lt;T&gt;"/> instance with the given timeout value.
    /// </summary>
    /// <param name="timeout">Time until an entry within this instance will expire, in seconds.</param>
    public RateLimit(int timeout) {
        if (timeout < 0) throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout valie cannot be negative.");
        Timeout = timeout;
    }

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

        if (Entries.ContainsKey(value)) return false;
        else {
            Entries.Add(value, DateTime.Now);
            return true;
        }
    }
}
