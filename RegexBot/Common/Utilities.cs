using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System.Diagnostics.CodeAnalysis;

namespace RegexBot.Common;
/// <summary>
/// Miscellaneous utility methods useful for the bot and modules.
/// </summary>
public static class Utilities {
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
}
