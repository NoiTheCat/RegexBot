using Discord;
using RegexBot.Data;

namespace RegexBot.Services.EntityCache;
class MessageCachingSubservice {
    private readonly RegexbotClient _bot;

    internal MessageCachingSubservice(RegexbotClient bot) {
        _bot = bot;
        bot.DiscordClient.MessageReceived += DiscordClient_MessageReceived;
        bot.DiscordClient.MessageUpdated += DiscordClient_MessageUpdated;
    }

    private Task DiscordClient_MessageReceived(SocketMessage arg)
        => AddOrUpdateCacheItemAsync(arg, false);
    private Task DiscordClient_MessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3) {
        // This event is fired also when a link preview embed is added to a message. In those situations, the message's edited timestamp
        // remains null, in addition to having other unusual and unexpected properties. We are not interested in these.
        if (!arg2.EditedTimestamp.HasValue) return Task.CompletedTask;
        return AddOrUpdateCacheItemAsync(arg2, true);
    }

    private async Task AddOrUpdateCacheItemAsync(SocketMessage arg, bool isUpdate) {
        //if (!Common.Utilities.IsValidUserMessage(arg, out _)) return;
        if (arg.Channel is not SocketTextChannel) return;
        if (arg.Author.IsWebhook) return; // do get bot messages, don't get webhooks
        if (((IMessage)arg).Type != MessageType.Default) return;
        if (arg is SocketSystemMessage) return;

        using var db = new BotDatabaseContext();
        CachedGuildMessage? cachedMsg = db.GuildMessageCache.Where(m => m.MessageId == (long)arg.Id).SingleOrDefault();

        if (isUpdate) {
            // Alternative for Discord.Net's MessageUpdated handler:
            // Notify subscribers of message update using EC entry for the previous message state
            var oldMsg = CachedGuildMessage.Clone(cachedMsg);
            var updEvent = new MessageCacheUpdateEvent(oldMsg, arg);
            await _bot.PushSharedEventAsync(updEvent);
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
}
