using System.Threading.Tasks;

namespace Kerobot.Services.EntityCache
{
    /// <summary>
    /// Provides and maintains a database-backed cache of entities. Portions of information collected by this
    /// service may be used by modules, while other portions are useful only for external applications which may
    /// require this information, such as an external web interface.
    /// </summary>
    class EntityCacheService : Service
    {
        private readonly UserCache _uc;

        internal EntityCacheService(Kerobot kb) : base(kb)
        {
            // Currently we only have UserCache. May add Channel and Server caches later.
            _uc = new UserCache(kb);
        }

        /// <summary>
        /// See documentation in Kerobot_hooks.
        /// </summary>
        internal Task<CachedUser> QueryUserCache(ulong guildId, string search) => _uc.Query(guildId, search);
    }
}
