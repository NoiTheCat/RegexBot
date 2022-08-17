using Discord;
using Microsoft.EntityFrameworkCore;
using RegexBot.Data;
using System.Text;

namespace RegexBot.Modules.ModLogs;
// Contains handlers and all logic relating to logging message edits and deletions
internal partial class ModLogs {
    const string PreviewCutoffNotify = "**Message too long to preview; showing first {0} characters.**\n\n";
    const string NotCached = "Message not cached.";
    const string MessageContentNull = "(blank)";

    private async Task HandleDelete(Cacheable<IMessage, ulong> argMsg, Cacheable<IMessageChannel, ulong> argChannel) {
        const int MaxPreviewLength = 750;
        if (argChannel.Value is not SocketTextChannel channel) return;
        var conf = GetGuildState<ModuleConfig>(channel.Guild.Id);
        var reportChannel = conf?.ReportingChannel?.FindChannelIn(channel.Guild, true);
        if (reportChannel == null) return;
        if ((conf?.LogMessageDeletions ?? false) == false) return;
        if (reportChannel.Id == channel.Id) {
            Log($"[{channel.Guild.Name}] Message deletion detected in the reporting channel. Regular report has been suppressed.");
            return;
        }

        using var db = new BotDatabaseContext();
        var cachedMsg = db.GuildMessageCache
            .Include(gm => gm.Author)
            .Where(gm => gm.MessageId == (long)argMsg.Id)
            .SingleOrDefault();

        var reportEmbed = new EmbedBuilder()
            .WithTitle("Message deleted")
            .WithCurrentTimestamp()
            .WithFooter($"User ID: {(cachedMsg == null ? "Unknown" : cachedMsg.AuthorId)}");

        if (cachedMsg != null) {
            if (cachedMsg.Content == null) {
                reportEmbed.Description = MessageContentNull;
            } else if (cachedMsg.Content.Length > MaxPreviewLength) {
                reportEmbed.Description = string.Format(PreviewCutoffNotify, MaxPreviewLength) +
                    cachedMsg.Content[MaxPreviewLength..];
            } else {
                reportEmbed.Description = cachedMsg.Content;
            }
            if (cachedMsg.Author == null) {
                reportEmbed.Author = new EmbedAuthorBuilder() {
                    Name = $"User ID {cachedMsg.AuthorId}",
                    IconUrl = GetDefaultAvatarUrl("0")
                };
            } else {
                reportEmbed.Author = new EmbedAuthorBuilder() {
                    Name = $"{cachedMsg.Author.Username}#{cachedMsg.Author.Discriminator}",
                    IconUrl = cachedMsg.Author.AvatarUrl ?? GetDefaultAvatarUrl(cachedMsg.Author.Discriminator)
                };
            }
            var attach = CheckAttachments(cachedMsg.AttachmentNames);
            if (attach != null) reportEmbed.AddField(attach);
        } else {
            reportEmbed.Description = NotCached;
        }
        
        var contextStr = new StringBuilder();
        contextStr.AppendLine($"User: {(cachedMsg != null ? $"<@!{cachedMsg.AuthorId}>" : "Unknown")}");
        contextStr.AppendLine($"Channel: <#{channel.Id}> (#{channel.Name})");
        contextStr.AppendLine($"Posted: {MakeTimestamp(SnowflakeUtils.FromSnowflake(argMsg.Id))}");
        if (cachedMsg?.EditedAt != null) contextStr.AppendLine($"Last edit: {MakeTimestamp(cachedMsg.EditedAt.Value)}");
        contextStr.AppendLine($"Message ID: {argMsg.Id}");
        reportEmbed.AddField(new EmbedFieldBuilder() {
            Name = "Context",
            Value = contextStr.ToString()
        });

        await reportChannel.SendMessageAsync(embed: reportEmbed.Build());
    }

    private async Task FilterIncomingEvents(ISharedEvent ev) {
        if (ev is MessageCacheUpdateEvent upd) {
            await HandleUpdate(upd.OldMessage, upd.NewMessage);
        }
    }

    private async Task HandleUpdate(CachedGuildMessage? oldMsg, SocketMessage newMsg) {
        const int MaxPreviewLength = 500;
        var channel = (SocketTextChannel)newMsg.Channel;
        var conf = GetGuildState<ModuleConfig>(channel.Guild.Id);
        var reportChannel = conf?.ReportingChannel?.FindChannelIn(channel.Guild, true);
        if (reportChannel == null) return;
        if ((conf?.LogMessageEdits ?? false) == false) return;
        if (reportChannel.Id == channel.Id) {
            Log($"[{channel.Guild.Name}] Message edit detected in the reporting channel. Regular report has been suppressed.");
            return;
        }

        var reportEmbed = new EmbedBuilder()
            .WithTitle("Message edited")
            .WithCurrentTimestamp()
            .WithFooter($"User ID: {newMsg.Author.Id}");

        reportEmbed.Author = new EmbedAuthorBuilder() {
            Name = $"{newMsg.Author.Username}#{newMsg.Author.Discriminator}",
            IconUrl = newMsg.Author.GetAvatarUrl() ?? newMsg.Author.GetDefaultAvatarUrl()
        };

        var oldField = new EmbedFieldBuilder() { Name = "Old" };
        if (oldMsg != null) {
            if (oldMsg.Content == null) {
                oldField.Value = MessageContentNull;
            } else if (oldMsg.Content.Length > MaxPreviewLength) {
                oldField.Value = string.Format(PreviewCutoffNotify, MaxPreviewLength) +
                    oldMsg.Content[MaxPreviewLength..];
            } else {
                oldField.Value = oldMsg.Content;
            }
        } else {
            oldField.Value = NotCached;
        }
        reportEmbed.AddField(oldField);

        // TODO shorten 'new' preview, add clickable? check if this would be good usability-wise
        var newField = new EmbedFieldBuilder() { Name = "New" };
        if (newMsg.Content == null) {
            newField.Value = MessageContentNull;
        } else if (newMsg.Content.Length > MaxPreviewLength) {
            newField.Value = string.Format(PreviewCutoffNotify, MaxPreviewLength) +
                newMsg.Content[MaxPreviewLength..];
        } else {
            newField.Value = newMsg.Content;
        }
        reportEmbed.AddField(newField);

        var attach = CheckAttachments(newMsg.Attachments.Select(a => a.Filename));
        if (attach != null) reportEmbed.AddField(attach);
        
        var contextStr = new StringBuilder();
        contextStr.AppendLine($"User: <@!{newMsg.Author.Id}>");
        contextStr.AppendLine($"Channel: <#{channel.Id}> (#{channel.Name})");
        if ((oldMsg?.EditedAt) == null) contextStr.AppendLine($"Posted: {MakeTimestamp(SnowflakeUtils.FromSnowflake(newMsg.Id))}");
        else contextStr.AppendLine($"Previous edit: {MakeTimestamp(oldMsg.EditedAt.Value)}");
        contextStr.AppendLine($"Message ID: {newMsg.Id}");
        var contextField = new EmbedFieldBuilder() {
            Name = "Context",
            Value = contextStr.ToString()
        };
        reportEmbed.AddField(contextField);

        await reportChannel.SendMessageAsync(embed: reportEmbed.Build());
    }

    private static EmbedFieldBuilder? CheckAttachments(IEnumerable<string> attachments) {
        if (attachments.Any()) {
            var field = new EmbedFieldBuilder { Name = "Attachments" };
            var attachNames = new StringBuilder();
            foreach (var name in attachments) {
                attachNames.AppendLine($"`{name}`");
            }
            field.Value = attachNames.ToString().TrimEnd();
            return field;
        }
        return null;
    }
}