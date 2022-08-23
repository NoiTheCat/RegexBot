namespace RegexBot.Common;
/// <summary>
/// Helper class that holds an entity's name, ID, or both.
/// Meant to be used during configuration processing in cases where the configuration expects
/// an entity name to be defined in a certain way which may or may not include its snowflake ID.
/// </summary>
public class EntityName {
    /// <summary>
    /// The entity's type, as specified in configuration.
    /// </summary>
    public EntityType Type { get; private set; }

    private ulong? _id;
    /// <summary>
    /// Entity's unique ID value (snowflake). May be null if the value is not known.
    /// </summary>
    /// <remarks>
    /// This value may be updated during runtime if the parent <see cref="EntityList"/> was instructed to
    /// update the ID for persistence.
    /// </remarks>
    public ulong? Id {
        get => _id;
        internal set => _id ??= value;
    }

    /// <summary>
    /// Entity's name as specified in configuration. May be null if it was not specified.
    /// </summary>
    /// <remarks>This value is not updated during runtime.</remarks>
    public string? Name { get; private set; }

    /// <summary>
    /// Creates a new object instance from the given input string.
    /// Documentation for the EntityName format can be found elsewhere in this project's documentation.
    /// </summary>
    /// <param name="input">Input string in EntityName format.</param>
    /// <exception cref="ArgumentNullException">Input string is null or blank.</exception>
    /// <exception cref="ArgumentException">Input string cannot be resolved to an entity type.</exception>
    public EntityName(string input) {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentNullException(nameof(input), "Specified name is blank.");

        // Check if type prefix was specified and extract it
        Type = default;
        if (input.Length >= 2) {
            if (input[0] == '&') Type = EntityType.Role;
            else if (input[0] == '#') Type = EntityType.Channel;
            else if (input[0] == '@') Type = EntityType.User;
        }
        if (Type == default)
            throw new ArgumentException("Entity type unable to be inferred by given input.");

        input = input[1..]; // Remove prefix

        // Input contains ID/Label separator?
        var separator = input.IndexOf("::");
        if (separator != -1) {
            Name = input[(separator + 2)..];
            if (ulong.TryParse(input.AsSpan(0, separator), out var parseOut)) {
                // Got an ID.
                Id = parseOut;
            } else {
                // It's not actually an ID. Assuming the entire string is a name.
                Name = input;
                Id = null;
            }
        } else {
            // No separator. Input is either entirely an ID or entirely a Name.
            if (ulong.TryParse(input, out var parseOut)) {
                // ID without name.
                Id = parseOut;
                Name = null;
            } else {
                // Name without ID.
                Name = input;
                Id = null;
            }
        }
    }

    /// <summary>
    /// Creates a new object instance from the given input string.
    /// Documentation for the EntityName format can be found elsewhere in this project's documentation.
    /// </summary>
    /// <param name="input">Input string in EntityName format.</param>
    /// <param name="expectedType">The <see cref="EntityType"/> expected for this instance.</param>
    /// <exception cref="ArgumentNullException">Input string is null or blank.</exception>
    /// <exception cref="ArgumentException">Input string cannot be resolved to an entity type.</exception>
    /// <exception cref="FormatException">Input string was resolved to a type other than specified.</exception>
    public EntityName(string input, EntityType expectedType) : this(input) {
        if (Type != expectedType) throw new FormatException("Resolved EntityType does not match expected type.");
    }

    /// <summary>
    /// Returns the appropriate prefix corresponding to an EntityType.
    /// </summary>
    public static char GetPrefix(EntityType t) => t switch {
        EntityType.Role => '&',
        EntityType.Channel => '#',
        EntityType.User => '@',
        _ => '\0',
    };

    /// <summary>
    /// Returns a string representation of this item in proper EntityName format.
    /// </summary>
    public override string ToString() {
        var pf = GetPrefix(Type);

        if (Id.HasValue && Name != null)
            return $"{pf}{Id.Value}::{Name}";
        else if (Id.HasValue)
            return $"{pf}{Id}";
        else
            return $"{pf}{Name}";
    }

    #region Helper methods
    /// <summary>
    /// Attempts to find the corresponding role within the given guild.
    /// </summary>
    /// <param name="guild">The guild in which to search for the role.</param>
    /// <param name="updateMissingID">
    /// Specifies if this EntityName instance should cache the snowflake ID of the
    /// corresponding role found in this guild if it is not already known by this instance.
    /// </param>
    public SocketRole? FindRoleIn(SocketGuild guild, bool updateMissingID = false) {
        if (Type != EntityType.Role)
            throw new ArgumentException("This EntityName instance must correspond to a Role.");

        var dirty = false; // flag for updating ID if possible regardless of updateMissingId setting
        if (Id.HasValue) {
            var role = guild.GetRole(Id.Value);
            if (role != null) return role;
            else dirty = true; // only set if ID already existed but is now invalid
        }

        var r = guild.Roles.FirstOrDefault(rq => string.Equals(rq.Name, Name, StringComparison.OrdinalIgnoreCase));
        if (r != null && (updateMissingID || dirty)) Id = r.Id;

        return r;
    }

    /// <summary>
    /// Attempts to find the corresponding user within the given guild.
    /// </summary>
    /// <param name="guild">The guild in which to search for the user.</param>
    /// <param name="updateMissingID">
    /// Specifies if this EntityName instance should cache the snowflake ID of the
    /// corresponding user found in this guild if it is not already known by this instance.
    /// </param>
    public SocketGuildUser? FindUserIn(SocketGuild guild, bool updateMissingID = false) {
        if (Type != EntityType.User)
            throw new ArgumentException("This EntityName instance must correspond to a User.");

        var dirty = false; // flag for updating ID if possible regardless of updateMissingId setting
        if (Id.HasValue) {
            var user = guild.GetUser(Id.Value);
            if (user != null) return user;
            else dirty = true; // only set if ID already existed but is now invalid
        }

        var u = guild.Users.FirstOrDefault(rq => string.Equals(rq.Username, Name, StringComparison.OrdinalIgnoreCase));
        if (u != null && (updateMissingID || dirty)) Id = u.Id;

        return u;
    }

    /// <summary>
    /// Attempts to find the corresponding channel within the given guild.
    /// </summary>
    /// <param name="guild">The guild in which to search for the channel.</param>
    /// <param name="updateMissingID">
    /// Specifies if this EntityName instance should cache the snowflake ID of the
    /// corresponding channel found in this guild if it is not already known by this instance.
    /// </param>
    public SocketTextChannel? FindChannelIn(SocketGuild guild, bool updateMissingID = false) {
        if (Type != EntityType.Channel)
            throw new ArgumentException("This EntityName instance must correspond to a Channel.");

        var dirty = false; // flag for updating ID if possible regardless of updateMissingId setting
        if (Id.HasValue) {
            var channel = guild.GetTextChannel(Id.Value);
            if (channel != null) return channel;
            else dirty = true; // only set if ID already existed but is now invalid
        }

        var c = guild.TextChannels.FirstOrDefault(rq => string.Equals(rq.Name, Name, StringComparison.OrdinalIgnoreCase));
        if (c != null && (updateMissingID || dirty)) Id = c.Id;

        return c;
    }
    #endregion
}
