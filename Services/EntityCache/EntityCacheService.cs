using RegexBot.Data;

namespace RegexBot.Services.EntityCache;
/// <summary>
/// Provides and maintains a database-backed cache of entities.
/// </summary>
class EntityCacheService : Service {
    private readonly UserCachingSubservice _uc;
    #pragma warning disable IDE0052
    private readonly MessageCachingSubservice _mc;
    #pragma warning restore IDE0052

    internal EntityCacheService(RegexbotClient bot) : base(bot) {
        _uc = new UserCachingSubservice(bot, Log);
        _mc = new MessageCachingSubservice(bot);
    }

    // Hooked
    internal CachedUser? QueryUserCache(string search)
        => _uc.DoUserQuery(search);

    // Hooked
    internal CachedGuildUser? QueryGuildUserCache(ulong guildId, string search)
        => _uc.DoGuildUserQuery(guildId, search);
}
