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
                // User has defined both keys at once, which is not allowed
                throw new FormatException($"Cannot have both '{whitelistKey}' and '{blacklistKey}' defined at once.");
        }

        JToken? incoming = null;
        if (whitelistKey != null) {
            // Try getting a whitelist
            incoming = config[whitelistKey];
            Mode = FilterMode.Whitelist;
        }
        if (incoming == null && blacklistKey != null) {
            // Try getting a blacklist
            incoming = config[blacklistKey];
            Mode = FilterMode.Blacklist;
        }

        if (incoming == null) {
            // Got neither. Have an empty list.
            Mode = FilterMode.None;
            FilteredList = new EntityList();
            FilterExemptions = new EntityList();
            return;
        }

        if (incoming.Type != JTokenType.Array)
            throw new FormatException("Filtering list must be a JSON array.");
        FilteredList = new EntityList((JArray)incoming);

        // Verify the same for the exemption list.
        if (exemptKey != null) {
            var incomingEx = config[exemptKey];
            if (incomingEx == null) {
                FilterExemptions = new EntityList();
            } else if (incomingEx.Type != JTokenType.Array) {
                throw new FormatException("Filtering exemption list must be a JSON array.");
            } else {
                FilterExemptions = new EntityList(incomingEx);
            }
        } else {
            FilterExemptions = new EntityList();
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
