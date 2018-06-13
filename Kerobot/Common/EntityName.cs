using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;

namespace Kerobot.Common
{
    /// <summary>
    /// The type of entity specified in an <see cref="EntityName"/>.
    /// </summary>
    public enum EntityType { Unspecified, Role, Channel, User }

    /// <summary>
    /// Helper class that holds an entity's name, ID, or both.
    /// Meant to be used during configuration processing in cases where the configuration expects
    /// an entity name to be defined in a certain way and may or may not include its snowflake ID.
    /// </summary>
    public class EntityName
    {
        /// <summary>
        /// The entity's type, if specified in configuration.
        /// </summary>
        public EntityType Type { get; private set; }

        /// <summary>
        /// Entity's unique ID value (snowflake). May be null if the value is not known.
        /// </summary>
        public ulong? Id { get; private set; }

        /// <summary>
        /// Entity's name as specified in configuration. May be null if it was not specified.
        /// This value is not updated during runtime.
        /// </summary>
        public string Name { get; private set; }
        

        // TODO elsewhere: find a way to emit a warning if the user specified a name without ID in configuration.

        /// <summary>
        /// Creates a new object instance from the given input string.
        /// Documentation for the EntityName format can be found elsewhere in this project's documentation.
        /// </summary>
        /// <param name="input">Input string in EntityName format.</param>
        /// <exception cref="ArgumentNullException">Input string is null or blank.</exception>
        public EntityName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentNullException("Specified name is null or blank.");

            // Check if type prefix was specified and extract it
            Type = EntityType.Unspecified;
            if (input.Length >= 2)
            {
                if (input[0] == '&') Type = EntityType.Role;
                else if (input[0] == '#') Type = EntityType.Channel;
                else if (input[0] == '@') Type = EntityType.User;

                if (Type != EntityType.Unspecified) input = input.Substring(1);
            }

            // Input contains ID/Label separator?
            int separator = input.IndexOf("::");
            if (separator != -1)
            {
                Name = input.Substring(separator + 2, input.Length - (separator + 2));
                if (ulong.TryParse(input.Substring(0, separator), out var parseOut))
                {
                    // Got an ID.
                    Id = parseOut;
                }
                else
                {
                    // It's not actually an ID. Assuming the entire string is a name.
                    Name = input;
                    Id = null;
                }
            }
            else
            {
                // No separator. Input is either entirely an ID or entirely a Name.
                if (ulong.TryParse(input, out var parseOut))
                {
                    // ID without name.
                    Id = parseOut;
                    Name = null;
                }
                else
                {
                    // Name without ID.
                    Name = input;
                    Id = null;
                }
            }
        }

        #region Name to ID resolving
        /// <summary>
        /// Attempts to determine the corresponding ID if not already known.
        /// Searches the given guild for it and stores it into this instance if found.
        /// Immediately returns the ID if it is already known by this instance.
        /// </summary>
        /// <param name="searchType">The entity type to which this instance corresponds to.</param>
        /// <param name="id">If known, outputs the ID of the corresponding entity.</param>
        /// <returns>True if the ID is known.</returns>
        public bool TryResolve(SocketGuild searchGuild, out ulong id, EntityType searchType = EntityType.Unspecified)
        {
            if (Id.HasValue)
            {
                id = Id.Value;
                return true;
            }
            if (searchType != EntityType.Unspecified && Type == EntityType.Unspecified) Type = searchType;
            if (string.IsNullOrWhiteSpace(Name))
            {
                id = default;
                return false;
            }

            Predicate<ISnowflakeEntity> resolver;
            IEnumerable<ISnowflakeEntity> collection;
            switch (Type)
            {
                case EntityType.Role:
                    collection = searchGuild.Roles;
                    resolver = ResolveTryRole;
                    break;
                case EntityType.Channel:
                    collection = searchGuild.TextChannels;
                    resolver = ResolveTryChannel;
                    break;
                case EntityType.User:
                    collection = searchGuild.Users;
                    resolver = ResolveTryUser;
                    break;
                default:
                    id = default;
                    return false;
            }

            foreach (var item in collection)
            {
                if (resolver.Invoke(item))
                {
                    Id = item.Id;
                    id = Id.Value;
                    return true;
                }
            }

            id = default;
            return false;
        }

        private bool ResolveTryRole(ISnowflakeEntity entity)
        {
            var r = (SocketRole)entity;
            return string.Equals(r.Name, this.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool ResolveTryChannel(ISnowflakeEntity entity)
        {
            var c = (SocketTextChannel)entity;
            return string.Equals(c.Name, this.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool ResolveTryUser(ISnowflakeEntity entity)
        {
            var u = (SocketGuildUser)entity;
            // Check username first, then nickname
            return string.Equals(u.Username, this.Name, StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(u.Nickname, this.Name, StringComparison.InvariantCultureIgnoreCase);
        }
        #endregion

        /// <summary>
        /// Returns the appropriate prefix corresponding to an EntityType.
        /// </summary>
        public static string Prefix(EntityType t)
        {
            switch (t)
            {
                case EntityType.Role: return "&";
                case EntityType.Channel: return "#";
                case EntityType.User: return "@";
                default: return "";
            }
        }

        /// <summary>
        /// Returns a string representation of this EntityName, in EntityName format.
        /// </summary>
        public override string ToString()
        {
            string pf = Prefix(Type);

            if (Id.HasValue && Name != null)
                return $"{pf}{Id.Value}::{Name}";
            else if (Id.HasValue)
                return $"{pf}{Id}";
            else
                return $"{pf}{Name}";
        }
    }
}
