using System;

namespace Kerobot.Services.EntityCache
{
    /// <summary>
    /// Provides and maintains a database-backed cache of users.
    /// It is meant to work as an addition to Discord.Net's own user caching capabilities, and its main purpose
    /// is to be able to provide basic information on users which the bot may not currently be aware about.
    /// </summary>
    class EntityCacheService : Service
    {
        public EntityCacheService(Kerobot kb) : base(kb)
        {
        }
    }
}
