using RegexBot.Data;

namespace RegexBot.Services.EntityCache;
/// <summary>
/// Provides and maintains a database-backed cache of entities. Portions of information collected by this
/// service may be used by modules, while other portions are useful only for external applications which may
/// require this information, such as an external web interface.
/// </summary>
class EntityCacheService : Service {
    private readonly UserCachingSubservice _uc;
    private readonly MessageCachingSubservice _mc;

    internal EntityCacheService(RegexbotClient bot) : base(bot) {
        // Currently we only have UserCache. May add Channel and Server caches later.
        _uc = new UserCachingSubservice(bot);
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
