using Discord;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace RegexBot.Common;
/// <summary>
/// Miscellaneous utility methods useful for the bot and modules.
/// </summary>
public static class Utilities {
    /// <summary>
    /// Gets a compiled regex that matches a channel tag and pulls its snowflake value.
    /// </summary>
    public static Regex ChannelMention { get; } = new(@"<#(?<snowflake>\d+)>", RegexOptions.Compiled);

    /// <summary>
    /// Gets a compiled regex that matches a custom emoji and pulls its name and ID.
    /// </summary>
    public static Regex CustomEmoji { get; } = new(@"<:(?<name>[A-Za-z0-9_]{2,}):(?<ID>\d+)>", RegexOptions.Compiled);

    /// <summary>
    /// Gets a compiled regex that matches a fully formed Discord handle, extracting the name and discriminator.
    /// </summary>
    public static Regex DiscriminatorSearch { get; } = new(@"(.+)#(\d{4}(?!\d))", RegexOptions.Compiled);

    /// <summary>
    /// Gets a compiled regex that matches a user tag and pulls its snowflake value.
    /// </summary>
    public static Regex UserMention { get; } = new(@"<@!?(?<snowflake>\d+)>", RegexOptions.Compiled);

    /// <summary>
    /// Performs common checks on the specified message to see if it fits all the criteria of a
    /// typical, ordinary message sent by an ordinary guild user.
    /// </summary>
    public static bool IsValidUserMessage(SocketMessage msg, [NotNullWhen(true)] out SocketTextChannel? channel) {
        channel = default;
        if (msg.Channel is not SocketTextChannel ch) return false;
        if (msg.Author.IsBot || msg.Author.IsWebhook) return false;
        if (((IMessage)msg).Type != MessageType.Default) return false;
        if (msg is SocketSystemMessage) return false;
        channel = ch;
        return true;
    }

    /// <summary>
    /// Given a JToken, gets all string-based values out of it if the token may be a string
    /// or an array of strings.
    /// </summary>
    /// <param name="token">The JSON token to analyze and retrieve strings from.</param>
    /// <exception cref="ArgumentException">Thrown if the given token is not a string or array containing all strings.</exception>
    /// <exception cref="ArgumentNullException">Thrown if the given token is null.</exception>
    public static List<string> LoadStringOrStringArray(JToken? token) {
        const string ExNotString = "This token contains a non-string element.";
        if (token == null) throw new ArgumentNullException(nameof(token), "The provided token is null.");
        var results = new List<string>();
        if (token.Type == JTokenType.String) {
            results.Add(token.Value<string>()!);
        } else if (token.Type == JTokenType.Array) {
            foreach (var entry in token.Values()) {
                if (entry.Type != JTokenType.String) throw new ArgumentException(ExNotString, nameof(token));
                results.Add(entry.Value<string>()!);
            }
        } else {
            throw new ArgumentException(ExNotString, nameof(token));
        }
        return results;
    }

    /// <summary>
    /// Builds and returns an embed which displays this log entry.
    /// </summary>
    public static Embed BuildEmbed(this Data.ModLogEntry entry, RegexbotClient bot) {
        var logEmbed = new EmbedBuilder()
            .WithTitle("Moderation log entry")
            .WithTimestamp(entry.Timestamp)
            .WithFooter($"Log ID {entry.LogId}");

        string? issuedDisplay = null;
        try {
            var entityTry = new EntityName(entry.IssuedBy, EntityType.User);
            var issueq = bot.EcQueryUser(entityTry.Id!.Value.ToString());
            if (issueq != null) issuedDisplay = $"<@{issueq.UserId}> - {issueq.Username}#{issueq.Discriminator} `{issueq.UserId}`";
            else issuedDisplay = $"Unknown user with ID `{entityTry.Id!.Value}`";
        } catch (Exception) { }
        issuedDisplay ??= entry.IssuedBy;
        string targetDisplay;
        var targetq = bot.EcQueryUser(entry.UserId.ToString());
        if (targetq != null) targetDisplay = $"<@{targetq.UserId}> - {targetq.Username}#{targetq.Discriminator} `{targetq.UserId}`";
        else targetDisplay = $"Unknown user with ID `{entry.UserId}`";

        var contextStr = new StringBuilder();
        contextStr.AppendLine($"Log type: {Enum.GetName(typeof(ModLogType), entry.LogType)}");
        contextStr.AppendLine($"Regarding user: {targetDisplay}");
        contextStr.AppendLine($"Logged by: {issuedDisplay}");

        logEmbed.AddField(new EmbedFieldBuilder() {
            Name = "Context",
            Value = contextStr.ToString()
        });
        if (entry.Message != null) {
            logEmbed.AddField(new EmbedFieldBuilder() {
                Name = "Message",
                Value = entry.Message
            });
        }

        return logEmbed.Build();
    }
}
