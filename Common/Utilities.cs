using Discord;
using RegexBot.Data;
using System.Diagnostics.CodeAnalysis;
using System.Net;
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
    /// Returns a representation of this entity that can be parsed by the <seealso cref="EntityName"/> constructor.
    /// </summary>
    public static string AsEntityNameString(this IUser entity) => $"@{entity.Id}::{entity.Username}";

    /// <summary>
    /// If given string is in an EntityName format, returns a displayable representation of it based on
    /// a cache query. Otherwise, returns the input string as-is.
    /// </summary>
    [return: NotNullIfNotNull("input")]
    public static string? TryFromEntityNameString(string? input, RegexbotClient bot) {
        string? result = null;
        try {
            var entityTry = new EntityName(input!, EntityType.User);
            var issueq = bot.EcQueryUser(entityTry.Id!.Value.ToString());
            if (issueq != null) result = $"<@{issueq.UserId}> - {issueq.GetDisplayableUsername()} `{issueq.UserId}`";
            else result = $"Unknown user with ID `{entityTry.Id!.Value}`";
        } catch (Exception) { }
        return result ?? input;
    }

    /// <inheritdoc cref="GetDisplayableUsernameCommon"/>
    public static string GetDisplayableUsername(this SocketUser user)
        => GetDisplayableUsernameCommon(user.Username, user.Discriminator, user.GlobalName);

    /// <inheritdoc cref="GetDisplayableUsernameCommon"/>
    public static string GetDisplayableUsername(this CachedUser user)
        => GetDisplayableUsernameCommon(user.Username, user.Discriminator, user.GlobalName);

    /// <summary>
    /// Returns a string representation of the user's username suitable for display purposes.
    /// For the sake of consistency, it is preferable using this instead of any other means, including Discord.Net's ToString.
    /// </summary>
    private static string GetDisplayableUsernameCommon(string username, string discriminator, string? global) {
        static string escapeFormattingCharacters(string input) {
            var result = new StringBuilder();
            foreach (var c in input) {
                if (c is '\\' or '_' or '~' or '*' or '@' or '`') {
                    result.Append('\\');
                }
                result.Append(c);
            }
            return result.ToString();
        }

        if (discriminator == "0000") {
            if (global != null) return $"{escapeFormattingCharacters(global)} ({username})";
            return username;
        } else {
            return $"{escapeFormattingCharacters(username)}#{discriminator}";
        }
    }
}
