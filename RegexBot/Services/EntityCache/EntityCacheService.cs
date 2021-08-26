using System.Threading.Tasks;

namespace RegexBot.Services.EntityCache
{
    /// <summary>
    /// Provides and maintains a database-backed cache of entities. Portions of information collected by this
    /// service may be used by modules, while other portions are useful only for external applications which may
    /// require this information, such as an external web interface.
    /// </summary>
    class EntityCacheService : Service
    {
        private readonly UserCache _uc;

        internal EntityCacheService(RegexbotClient bot) : base(bot)
        {
            // Currently we only have UserCache. May add Channel and Server caches later.
            _uc = new UserCache(bot);
        }

        /// <summary>
        /// See <see cref="RegexbotClient.EcQueryUser(ulong, string)"/>.
        /// </summary>
        internal Task<CachedUser> QueryUserCache(ulong guildId, string search) => _uc.Query(guildId, search);
    }
}
