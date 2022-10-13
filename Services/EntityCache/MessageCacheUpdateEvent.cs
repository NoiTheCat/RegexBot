using RegexBot.Data;

namespace RegexBot;
/// <summary>
/// Fired after a message edit, when the message cache is about to be updated with the edited message.
/// </summary>
/// <remarks>
/// Processing this serves as an alternative to <seealso cref="BaseSocketClient.MessageUpdated"/>,
/// pulling the previous state of the message from the entity cache instead of the library's cache.
/// </remarks>
public class MessageCacheUpdateEvent : ISharedEvent {
    /// <summary>
    /// Gets the previous state of the message prior to being updated, as known by the entity cache.
    /// </summary>
    public CachedGuildMessage? OldMessage { get; }

    /// <summary>
    /// Gets the new, updated incoming message.
    /// </summary>
    public SocketMessage NewMessage { get; }

    internal MessageCacheUpdateEvent(CachedGuildMessage? old, SocketMessage @new) {
        OldMessage = old;
        NewMessage = @new;
    }
}