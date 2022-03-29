using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace RegexBot.Common;

/// <summary>
/// Represents a commonly-used configuration structure: an array of strings consisting of <see cref="EntityName"/> values.
/// </summary>
public class EntityList : IEnumerable<EntityName> {
    private readonly IReadOnlyCollection<EntityName> _innerList;

    /// <summary>Gets an enumerable collection of all role names defined in this list.</summary>
    public IEnumerable<EntityName> Roles
        => _innerList.Where(n => n.Type == EntityType.Role);

    /// <summary>Gets an enumerable collection of all channel names defined in this list.</summary>
    public IEnumerable<EntityName> Channels
        => _innerList.Where(n => n.Type == EntityType.Channel);

    /// <summary>Gets an enumerable collection of all user names defined in this list.</summary>
    public IEnumerable<EntityName> Users
        => _innerList.Where(n => n.Type == EntityType.User);

    /// <summary>
    /// Creates a new EntityList instance with no data.
    /// </summary>
    public EntityList() : this(null, false) { }

    /// <summary>
    /// Creates a new EntityList instance using the given JSON token as input.
    /// </summary>
    /// <param name="input">JSON array to be used for input. For ease of use, null values are also accepted.</param>
    /// <param name="enforceTypes">Specifies if all entities defined in configuration must have their type specified.</param>
    /// <exception cref="ArgumentException">The input is not a JSON array.</exception>
    /// <exception cref="ArgumentNullException">
    /// Unintiutively, this exception is thrown if a user-provided configuration value is blank.
    /// </exception>
    /// <exception cref="FormatException">
    /// When enforceTypes is set, this is thrown if an EntityName results in having its Type be Unspecified.
    /// </exception>
    public EntityList(JToken? input, bool enforceTypes) {
        if (input == null) {
            _innerList = new List<EntityName>().AsReadOnly();
            return;
        }

        if (input.Type != JTokenType.Array)
            throw new ArgumentException("JToken input must be a JSON array.");
        var inputArray = (JArray)input;

        var list = new List<EntityName>();
        foreach (var item in inputArray.Values<string>()) {
            if (string.IsNullOrWhiteSpace(item)) continue;
            var itemName = new EntityName(item);
            if (enforceTypes && itemName.Type == EntityType.Unspecified)
                throw new FormatException($"The following value is not prefixed: {item}");
            list.Add(itemName);
        }
        _innerList = list.AsReadOnly();
    }

    /// <summary>
    /// Checks if the parameters of the given <see cref="SocketMessage"/> matches with
    /// any entity specified in this list.
    /// </summary>
    /// <param name="keepId">
    /// Specifies if EntityName instances within this list should have their internal ID value
    /// updated if found during the matching process.
    /// </param>
    /// <returns>
    /// True if the message author exists in this list, or if the message's channel exists in this list,
    /// or if the message author contains a role that exists in this list.
    /// </returns>
    public bool IsListMatch(SocketMessage msg, bool keepId) {
        var author = (SocketGuildUser)msg.Author;
        var authorRoles = author.Roles;
        var channel = msg.Channel;

        foreach (var entry in this) {
            if (entry.Type == EntityType.Role) {
                if (entry.Id.HasValue) {
                    return authorRoles.Any(r => r.Id == entry.Id.Value);
                } else {
                    foreach (var r in authorRoles) {
                        if (!string.Equals(r.Name, entry.Name, StringComparison.OrdinalIgnoreCase)) break;
                        if (keepId) entry.SetId(r.Id);
                        return true;
                    }
                }
            } else if (entry.Type == EntityType.Channel) {
                if (entry.Id.HasValue) {
                    return entry.Id.Value == channel.Id;
                } else {
                    if (!string.Equals(channel.Name, entry.Name, StringComparison.OrdinalIgnoreCase)) break;
                    if (keepId) entry.SetId(channel.Id);
                    return true;
                }
            } else // User
              {
                if (entry.Id.HasValue) {
                    return entry.Id.Value == author.Id;
                } else {
                    if (!string.Equals(author.Username, entry.Name, StringComparison.OrdinalIgnoreCase)) break;
                    if (keepId) entry.SetId(author.Id);
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Determines if this list contains no entries.
    /// </summary>
    public bool IsEmpty() => _innerList.Count == 0;

    public override string ToString() => $"Entity list contains {_innerList.Count} item(s).";

    public IEnumerator<EntityName> GetEnumerator() => _innerList.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
