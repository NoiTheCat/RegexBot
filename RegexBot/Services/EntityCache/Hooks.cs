using Discord.WebSocket;
using RegexBot.Data;
using RegexBot.Services.EntityCache;

namespace RegexBot;
partial class RegexbotClient {
    private readonly EntityCacheService _svcEntityCache;

    /// <summary>
    /// Queries the entity cache for user information. The given search string may contain a user ID
    /// or a username with optional discriminator. In case there are multiple results, the most recently
    /// cached user will be returned.
    /// </summary>
    /// <param name="search">Search string. May be a name with discriminator, a name, or an ID.</param>
    /// <returns>A <see cref="CachedUser"/> instance containing cached information, or null if no result.</returns>
    public CachedUser? EcQueryUser(string search) => _svcEntityCache.QueryUserCache(search);

    /// <summary>
    /// Queries the entity cache for guild-specific user information. The given search string may contain a user ID,
    /// nickname, or a username with optional discriminator. In case there are multiple results, the most recently
    /// cached user will be returned.
    /// </summary>
    /// <param name="guildId">ID of the corresponding guild in which to search.</param>
    /// <param name="search">Search string. May be a name with discriminator, a name, or an ID.</param>
    /// <returns>A <see cref="CachedGuildUser"/> instance containing cached information, or null if no result.</returns>
    public CachedGuildUser? EcQueryGuildUser(ulong guildId, string search) => _svcEntityCache.QueryGuildUserCache(guildId, search);

    /// <summary>
    /// Fired after a message edit, when the message cache is about to be updated with the edited message.
    /// </summary>
    /// <remarks>
    /// This event serves as an alternative to <seealso cref="BaseSocketClient.MessageUpdated"/>,
    /// pulling the previous state of the message from the entity cache instead of the library's cache.
    /// </remarks>
    public event EcMessageUpdateHandler? EcOnMessageUpdate {
        add { _svcEntityCache.OnCachePreUpdate += value; }
        remove { _svcEntityCache.OnCachePreUpdate -= value; }
    }

    /// <summary>
    /// Delegate used for the <seealso cref="EcOnMessageUpdate"/> event.
    /// </summary>
    /// <params>
    /// <param name="oldMsg">The previous state of the message prior to being updated, as known by the entity cache.</param>
    /// <param name="newMsg">The new, updated incoming message.</param>
    /// </params>
    public delegate Task EcMessageUpdateHandler(CachedGuildMessage? oldMsg, SocketMessage newMsg);
}
