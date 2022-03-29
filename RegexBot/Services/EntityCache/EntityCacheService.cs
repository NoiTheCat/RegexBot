using RegexBot.Data;

namespace RegexBot.Services.EntityCache;

/// <summary>
/// Provides and maintains a database-backed cache of entities. Portions of information collected by this
/// service may be used by modules, while other portions are useful only for external applications which may
/// require this information, such as an external web interface.
/// </summary>
class EntityCacheService : Service {
    private readonly UserCachingSubservice _uc;

    internal EntityCacheService(RegexbotClient bot) : base(bot) {
        // Currently we only have UserCache. May add Channel and Server caches later.
        _uc = new UserCachingSubservice(bot);
    }

    // Hooked
    internal static CachedUser? QueryUserCache(string search)
        => UserCachingSubservice.DoUserQuery(search);

    // Hooked
    internal static CachedGuildUser? QueryGuildUserCache(ulong guildId, string search)
        => UserCachingSubservice.DoGuildUserQuery(guildId, search);
}
