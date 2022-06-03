using Discord;
using Discord.WebSocket;
using System.Diagnostics.CodeAnalysis;

namespace RegexBot.Common;
/// <summary>
/// Miscellaneous useful functions that don't have a particular place anywhere else.
/// </summary>
public static class Misc {
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
}
