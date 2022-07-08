using Discord;
using Microsoft.EntityFrameworkCore;
using RegexBot.Data;
using System.Text;

namespace RegexBot.Modules.ModLogs;
// Contains handlers and all logic relating to logging message edits and deletions
public partial class ModLogs {
    const string PreviewCutoffNotify = "**Message too long to preview; showing first {0} characters.**\n\n";
    const string NotCached = "Message not cached.";

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
            .WithCurrentTimestamp();

        if (cachedMsg != null) {
            if (cachedMsg.Content.Length > MaxPreviewLength) {
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
            .WithCurrentTimestamp();
        Console.WriteLine(reportEmbed.Build().ToString());

        reportEmbed.Author = new EmbedAuthorBuilder() {
            Name = $"{newMsg.Author.Username}#{newMsg.Author.Discriminator}",
            IconUrl = newMsg.Author.GetAvatarUrl() ?? newMsg.Author.GetDefaultAvatarUrl()
        };
        Console.WriteLine(reportEmbed.Build().ToString());

        var oldField = new EmbedFieldBuilder() { Name = "Old" };
        if (oldMsg != null) {
            if (oldMsg.Content.Length > MaxPreviewLength) {
                oldField.Value = string.Format(PreviewCutoffNotify, MaxPreviewLength) +
                    oldMsg.Content[MaxPreviewLength..];
            } else {
                oldField.Value = oldMsg.Content;
            }
        } else {
            oldField.Value = NotCached;
        }
        reportEmbed.AddField(oldField);
        Console.WriteLine(reportEmbed.Build().ToString());

        // TODO shorten 'new' preview, add clickable? check if this would be good usability-wise
        var newField = new EmbedFieldBuilder() { Name = "New" };
        if (newMsg.Content.Length > MaxPreviewLength) {
            newField.Value = string.Format(PreviewCutoffNotify, MaxPreviewLength) +
                newMsg.Content[MaxPreviewLength..];
        } else {
            newField.Value = newMsg.Content;
        }
        reportEmbed.AddField(newField);
        Console.WriteLine(reportEmbed.Build().ToString());
        
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
        Console.WriteLine(reportEmbed.Build().ToString());

        await reportChannel.SendMessageAsync(embed: reportEmbed.Build());
    }
}