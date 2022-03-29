#pragma warning disable CA1822
using RegexBot.Data;
using RegexBot.Services.EntityCache;

namespace RegexBot;

partial class RegexbotClient {
    private EntityCacheService _svcEntityCache;

    /// <summary>
    /// Queries the entity cache for user information. The given search string may contain a user ID
    /// or a username with optional discriminator. In case there are multiple results, the most recently
    /// cached user will be returned.
    /// </summary>
    /// <param name="search">Search string. May be a name with discriminator, a name, or an ID.</param>
    /// <returns>A <see cref="CachedUser"/> instance containing cached information, or null if no result.</returns>
    public CachedUser? EcQueryUser(string search) => EntityCacheService.QueryUserCache(search);

    /// <summary>
    /// Queries the entity cache for guild-specific user information. The given search string may contain a user ID,
    /// nickname, or a username with optional discriminator. In case there are multiple results, the most recently
    /// cached user will be returned.
    /// </summary>
    /// <param name="guildId">ID of the corresponding guild in which to search.</param>
    /// <param name="search">Search string. May be a name with discriminator, a name, or an ID.</param>
    /// <returns>A <see cref="CachedGuildUser"/> instance containing cached information, or null if no result.</returns>
    public CachedGuildUser? EcQueryGuildUser(ulong guildId, string search) => EntityCacheService.QueryGuildUserCache(guildId, search);
}
