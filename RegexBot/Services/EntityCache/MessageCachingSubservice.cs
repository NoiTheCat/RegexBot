using Discord;
using Discord.WebSocket;
using RegexBot.Data;
using static RegexBot.RegexbotClient;

namespace RegexBot.Services.EntityCache;
class MessageCachingSubservice {
    // Hooked
    public event CachePreUpdateHandler? OnCachePreUpdate;

    private readonly Action<string, bool> _log;

    internal MessageCachingSubservice(RegexbotClient bot, Action<string, bool> logMethod) {
        _log = logMethod;
        bot.DiscordClient.MessageReceived += DiscordClient_MessageReceived;
        bot.DiscordClient.MessageUpdated += DiscordClient_MessageUpdated;
    }

    private Task DiscordClient_MessageReceived(SocketMessage arg) {
        if (arg.Channel is IDMChannel || arg is not SocketSystemMessage) return Task.CompletedTask;
        return AddOrUpdateCacheItemAsync(arg);
    }
    private Task DiscordClient_MessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3) {
        if (arg2.Channel is IDMChannel || arg2 is not SocketSystemMessage) return Task.CompletedTask;
        return AddOrUpdateCacheItemAsync(arg2);
    }

    private async Task AddOrUpdateCacheItemAsync(SocketMessage arg) {
        using var db = new BotDatabaseContext();

        CachedGuildMessage? msg = db.GuildMessageCache.Where(m => m.MessageId == (long)arg.Id).SingleOrDefault();
        if (msg == null) {
            msg = new() {
                MessageId = (long)arg.Id,
                AuthorId = (long)arg.Author.Id,
                GuildId = (long)((SocketGuildUser)arg.Author).Guild.Id,
                ChannelId = (long)arg.Channel.Id,
                AttachmentNames = arg.Attachments.Select(a => a.Filename).ToList(),
                Content = arg.Content
            };
            db.GuildMessageCache.Add(msg);
        } else {
            // Notify any listeners of cache update before it happens
            var oldMsg = msg.MemberwiseClone();
            await Task.Factory.StartNew(async () => await RunPreUpdateHandlersAsync(oldMsg));

            msg.EditedAt = DateTimeOffset.UtcNow;
            msg.Content = arg.Content;
            msg.AttachmentNames = arg.Attachments.Select(a => a.Filename).ToList();
            db.GuildMessageCache.Update(msg);
        }
        await db.SaveChangesAsync();
    }

    private async Task RunPreUpdateHandlersAsync(CachedGuildMessage msg) {
        CachePreUpdateHandler? eventList;
        lock (this) eventList = OnCachePreUpdate;
        if (eventList == null) return;

        foreach (var handler in eventList.GetInvocationList()) {
            try {
                await (Task)handler.DynamicInvoke(msg)!;
            } catch (Exception ex) {
                _log($"Unhandled exception in {nameof(RegexbotClient.OnCachePreUpdate)} handler '{handler.Method.Name}':", false);
                _log(ex.ToString(), false);
            }
        }
    }
}
