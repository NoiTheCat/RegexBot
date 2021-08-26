using RegexBot.Services.EntityCache;
using System.Threading.Tasks;

namespace RegexBot
{
    partial class RegexbotClient
    {
        private EntityCacheService _svcEntityCache;

        /// <summary>
        /// Queries the Entity Cache for user information. The given search string may contain a user ID
        /// or a username with optional discriminator. In case there are multiple results, the most recently
        /// cached user will be returned.
        /// </summary>
        /// <param name="guildId">ID of the corresponding guild in which to search.</param>
        /// <param name="search">Search string. May be a name with discriminator, a name, or an ID.</param>
        /// <returns>A <see cref="CachedUser"/> instance containing cached information, or null if no result.</returns>
        public Task<CachedUser> EcQueryUser(ulong guildId, string search) => _svcEntityCache.QueryUserCache(guildId, search);
    }
}
