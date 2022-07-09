using Discord;
using Discord.WebSocket;
using RegexBot.Data;
using static RegexBot.RegexbotClient;

namespace RegexBot.Services.EntityCache;
class MessageCachingSubservice {
    // Hooked
    public event EcMessageUpdateHandler? OnCachePreUpdate;

    private readonly Action<string, bool> _log;

    internal MessageCachingSubservice(RegexbotClient bot, Action<string, bool> logMethod) {
        _log = logMethod;
        bot.DiscordClient.MessageReceived += DiscordClient_MessageReceived;
        bot.DiscordClient.MessageUpdated += DiscordClient_MessageUpdated;
    }

    private Task DiscordClient_MessageReceived(SocketMessage arg)
        => AddOrUpdateCacheItemAsync(arg, false);
    private Task DiscordClient_MessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
        => AddOrUpdateCacheItemAsync(arg2, true);

    private async Task AddOrUpdateCacheItemAsync(SocketMessage arg, bool isUpdate) {
        if (!Common.Utilities.IsValidUserMessage(arg, out _)) return;

        using var db = new BotDatabaseContext();
        CachedGuildMessage? cachedMsg = db.GuildMessageCache.Where(m => m.MessageId == (long)arg.Id).SingleOrDefault();

        if (isUpdate) {
            // Alternative for Discord.Net's MessageUpdated handler:
            // Notify subscribers of message update using EC entry for the previous message state
            var oldMsg = cachedMsg?.MemberwiseClone();
            await Task.Factory.StartNew(async () => await RunPreUpdateHandlersAsync(oldMsg, arg));
        }

        if (cachedMsg == null) {
            cachedMsg = new() {
                MessageId = (long)arg.Id,
                AuthorId = (long)arg.Author.Id,
                GuildId = (long)((SocketGuildUser)arg.Author).Guild.Id,
                ChannelId = (long)arg.Channel.Id,
                AttachmentNames = arg.Attachments.Select(a => a.Filename).ToList(),
                Content = arg.Content
            };
            db.GuildMessageCache.Add(cachedMsg);
        } else {
            cachedMsg.EditedAt = DateTimeOffset.UtcNow;
            cachedMsg.Content = arg.Content;
            cachedMsg.AttachmentNames = arg.Attachments.Select(a => a.Filename).ToList();
            db.GuildMessageCache.Update(cachedMsg);
        }
        await db.SaveChangesAsync();
    }

    private async Task RunPreUpdateHandlersAsync(CachedGuildMessage? oldMsg, SocketMessage newMsg) {
        Delegate[]? subscribers;
        lock (this) {
            subscribers = OnCachePreUpdate?.GetInvocationList();
            if (subscribers == null || subscribers.Length == 0) return;
        }

        foreach (var handler in subscribers) {
            try {
                await (Task)handler.DynamicInvoke(oldMsg, newMsg)!;
            } catch (Exception ex) {
                _log($"Unhandled exception in {nameof(RegexbotClient.EcOnMessageUpdate)} handler '{handler.Method.Name}':", false);
                _log(ex.ToString(), false);
            }
        }
    }
}
