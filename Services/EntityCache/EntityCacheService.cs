using RegexBot.Data;

namespace RegexBot.Services.EntityCache;
/// <summary>
/// Provides and maintains a database-backed cache of entities.
/// </summary>
class EntityCacheService : Service {
    private readonly UserCachingSubservice _uc;
    private readonly MessageCachingSubservice _mc;

    internal EntityCacheService(RegexbotClient bot) : base(bot) {
        // Currently we only have UserCache. May add Channel and Server caches later.
        _uc = new UserCachingSubservice(bot, Log);
        _mc = new MessageCachingSubservice(bot, Log);
    }

    // Hooked
    internal CachedUser? QueryUserCache(string search)
        => _uc.DoUserQuery(search);

    // Hooked
    internal CachedGuildUser? QueryGuildUserCache(ulong guildId, string search)
        => _uc.DoGuildUserQuery(guildId, search);

    // Hooked
    internal event RegexbotClient.EcMessageUpdateHandler? OnCachePreUpdate {
        add { lock (_mc) _mc.OnCachePreUpdate += value; }
        remove { lock (_mc) _mc.OnCachePreUpdate -= value; }
    }
}
