namespace RegexBot.Common;
/// <summary>
/// Represents commonly-used configuration regarding whitelist/blacklist filtering, including exemptions.
/// </summary>
public class FilterList {
    /// <summary>
    /// The mode at which the <see cref="FilterList"/>'s filter criteria is operating.
    /// </summary>
    public enum FilterMode {
        /// <summary>
        /// A <see cref="FilterList"/> setting which does no filtering on the list.
        /// </summary>
        None,
        /// <summary>
        /// A <see cref="FilterList"/> setting which excludes only entites not in the list, excluding those exempted.
        /// </summary>
        Whitelist,
        /// <summary>
        /// A <see cref="FilterList"/> setting which allows all entities except those in the list, but allowing those exempted.
        /// </summary>
        Blacklist
    }

    /// <summary>
    /// Gets the mode at which the <see cref="FilterList"/>'s filter criteria is operating.
    /// </summary>
    public FilterMode Mode { get; }
    /// <summary>
    /// Gets the inner list that this instance is using for its filtering criteria.
    /// </summary>
    public EntityList FilteredList { get; }
    /// <summary>
    /// Gets the list of entities that may override filtering rules for this instance.
    /// </summary>
    public EntityList FilterExemptions { get; }

    /// <summary>
    /// Sets up a FilterList instance with the given JSON configuration section.
    /// </summary>
    /// <param name="config">
    /// JSON object in which to attempt to find the given whitelist, blacklist, and/or excemption keys.
    /// </param>
    /// <param name="whitelistKey">The key in which to search for the whitelist. Set as null to disable.</param>
    /// <param name="blacklistKey">The key in which to search for the blacklist. Set as null to disable.</param>
    /// <param name="exemptKey">The key in which to search for filter exemptions. Set as null to disable.</param>
    /// <exception cref="FormatException">
    /// Thrown if there is a problem with input. The exception message specifies the reason.
    /// </exception>
    public FilterList(JObject config, string whitelistKey = "Whitelist", string blacklistKey = "Blacklist", string exemptKey = "Exempt") {
        if (whitelistKey != null && config[whitelistKey] != null &&
            blacklistKey != null && config[blacklistKey] != null) {
            throw new FormatException($"Cannot have both '{whitelistKey}' and '{blacklistKey}' defined at once.");
        }

        JToken? valueSrc = null;
        if (whitelistKey != null) {
            // Try getting a whitelist
            valueSrc = config[whitelistKey];
            Mode = FilterMode.Whitelist;
        }
        if (valueSrc != null && blacklistKey != null) {
            // Try getting a blacklist
            valueSrc = config[blacklistKey];
            Mode = FilterMode.Blacklist;
        }

        if (valueSrc == null) {
            // Got neither. Have an empty list.
            Mode = FilterMode.None;
            FilteredList = new EntityList();
            FilterExemptions = new EntityList();
            return;
        }

        // Verify that the specified array is actually an array.
        if (valueSrc != null && valueSrc.Type != JTokenType.Array)
            throw new ArgumentException("Given list must be a JSON array.");
        FilteredList = new EntityList((JArray)valueSrc!, true);

        // Verify the same for the exemption list.
        FilterExemptions = new EntityList();
        if (exemptKey != null) {
            var exc = config[exemptKey];
            if (exc != null && exc.Type == JTokenType.Array) {
                FilterExemptions = new EntityList(exc, true);
            }
        }
    }

    /// <summary>
    /// Determines if the parameters of the given message match up against the filtering
    /// rules described within this instance.
    /// </summary>
    /// <param name="msg">
    /// The incoming message to be checked.
    /// </param>
    /// <param name="keepId">
    /// See equivalent documentation for <see cref="EntityList.IsListMatch(SocketMessage, bool)"/>.
    /// </param>
    /// <returns>
    /// True if the author or associated channel exists in and is not exempted by this instance.
    /// </returns>
    public bool IsFiltered(SocketMessage msg, bool keepId) {
        if (Mode == FilterMode.None) return false;

        var isInFilter = FilteredList.IsListMatch(msg, keepId);
        if (Mode == FilterMode.Whitelist) {
            if (!isInFilter) return true;
            return FilterExemptions.IsListMatch(msg, keepId);
        } else if (Mode == FilterMode.Blacklist) {
            if (!isInFilter) return false;
            return !FilterExemptions.IsListMatch(msg, keepId);
        }

        throw new Exception("it is not possible for this to happen");
    }
}
