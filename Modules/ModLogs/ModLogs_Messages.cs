using Discord;
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
        if ((conf?.LogMessageDeletions ?? false) == false) return;
        var reportChannel = conf?.ReportingChannel?.FindChannelIn(channel.Guild, true);
        if (reportChannel == null) return;
        if (reportChannel.Id == channel.Id) {
            Log(channel.Guild, "Message deleted in the reporting channel. Suppressing report.");
            return;
        }

        using var db = new BotDatabaseContext();
        var cachedMsg = db.GuildMessageCache
            .Where(gm => gm.MessageId == argMsg.Id)
            .SingleOrDefault();

        var reportEmbed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Message deleted")
            .WithCurrentTimestamp()
            .WithFooter($"Message ID: {argMsg.Id}");

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
                    Name = $"{cachedMsg.Author.User.Username}#{cachedMsg.Author.User.Discriminator}",
                    IconUrl = cachedMsg.Author.User.AvatarUrl ?? GetDefaultAvatarUrl(cachedMsg.Author.User.Discriminator)
                };
            }
            SetAttachmentsField(reportEmbed, cachedMsg.AttachmentNames);
        } else {
            reportEmbed.Description = NotCached;
        }

        var editLine = $"Posted: {MakeTimestamp(SnowflakeUtils.FromSnowflake(argMsg.Id))}";
        if (cachedMsg?.EditedAt != null) editLine += $"\nLast edit: {MakeTimestamp(cachedMsg.EditedAt.Value)}";
        SetContextField(reportEmbed, (ulong?)cachedMsg?.AuthorId, channel, editLine);

        await reportChannel.SendMessageAsync(embed: reportEmbed.Build());
    }

    private async Task HandleUpdate(CachedGuildMessage? oldMsg, SocketMessage newMsg) {
        const int MaxPreviewLength = 500;
        var channel = (SocketTextChannel)newMsg.Channel;
        var conf = GetGuildState<ModuleConfig>(channel.Guild.Id);
        
        var reportChannel = conf?.ReportingChannel?.FindChannelIn(channel.Guild, true);
        if (reportChannel == null) return;
        if (reportChannel.Id == channel.Id) {
            Log(channel.Guild, "Message edited in the reporting channel. Suppressing report.");
            return;
        }

        var reportEmbed = new EmbedBuilder()
            .WithColor(new Color(0xffff00)) // yellow
            .WithTitle("Message edited")
            .WithCurrentTimestamp()
            .WithFooter($"Message ID: {newMsg.Id}");

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

        SetAttachmentsField(reportEmbed, newMsg.Attachments.Select(a => a.Filename));

        string editLine;
        if ((oldMsg?.EditedAt) == null) editLine = $"Posted: {MakeTimestamp(SnowflakeUtils.FromSnowflake(newMsg.Id))}";
        else editLine = $"Previous edit: {MakeTimestamp(oldMsg.EditedAt.Value)}";
        SetContextField(reportEmbed, newMsg.Author.Id, channel, editLine);

        await reportChannel.SendMessageAsync(embed: reportEmbed.Build());
    }

    private void SetContextField(EmbedBuilder e, ulong? userId, SocketTextChannel channel, string editLine) {
        string userDisplay;
        if (userId.HasValue) {
            var q = Bot.EcQueryUser(userId.Value.ToString());
            if (q != null) userDisplay = $"<@{q.UserId}> - {q.Username}#{q.Discriminator} `{q.UserId}`";
            else userDisplay = $"Unknown user with ID `{userId}`";
        } else {
            userDisplay = "Unknown";
        }

        var contextStr = new StringBuilder();
        contextStr.AppendLine($"User: {userDisplay}");
        contextStr.AppendLine($"Channel: <#{channel.Id}> (#{channel.Name})");
        contextStr.AppendLine(editLine);

        e.AddField(new EmbedFieldBuilder() {
            Name = "Context",
            Value = contextStr.ToString()
        });
    }

    private static void SetAttachmentsField(EmbedBuilder e, IEnumerable<string> attachments) {
        if (attachments.Any()) {
            var field = new EmbedFieldBuilder { Name = "Attachments" };
            var attachNames = new StringBuilder();
            foreach (var name in attachments) {
                attachNames.AppendLine($"`{name}`");
            }
            field.Value = attachNames.ToString().TrimEnd();
            e.AddField(field);
        }
    }
}