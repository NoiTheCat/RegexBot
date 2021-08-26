using System;
using System.Data.Common;

namespace RegexBot // Publicly accessible class; placing in main namespace
{
    /// <summary>
    /// Representation of user information retrieved from Kerobot's UserCache.
    /// </summary>
    public class CachedUser
    {
        /// <summary>
        /// The user's snowflake ID.
        /// </summary>
        public ulong UserID { get; }

        /// <summary>
        /// The corresponding guild's snowflake ID.
        /// </summary>
        public ulong GuildID { get; }

        /// <summary>
        /// The date in which this user was first recorded onto the database.
        /// </summary>
        public DateTimeOffset FirstSeenDate { get; }

        /// <summary>
        /// The date in which cache information for this user was last updated.
        /// </summary>
        public DateTimeOffset CacheDate { get; }

        /// <summary>
        /// The user's corresponding username, without discriminator.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// The user's corresponding discriminator value.
        /// </summary>
        public string Discriminator { get; }

        /// <summary>
        /// The user's nickname in the corresponding guild. May be null.
        /// </summary>
        public string Nickname { get; }

        /// <summary>
        /// A link to a high resolution version of the user's current avatar. May be null.
        /// </summary>
        public string AvatarUrl { get; }

        internal CachedUser(DbDataReader row)
        {
            // Highly dependent on column order in the cache view defined in UserCacheService.
            unchecked
            {
                UserID = (ulong)row.GetInt64(0);
                GuildID = (ulong)row.GetInt64(1);
            }
            FirstSeenDate = row.GetDateTime(2).ToUniversalTime();
            CacheDate = row.GetDateTime(3).ToUniversalTime();
            Username = row.GetString(4);
            Discriminator = row.GetString(5);
            Nickname = row.IsDBNull(6) ? null : row.GetString(6);
            AvatarUrl = row.IsDBNull(7) ? null : row.GetString(7);
        }
    }
}
