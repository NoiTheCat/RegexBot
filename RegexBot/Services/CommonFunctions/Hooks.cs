﻿using Discord.WebSocket;
using RegexBot.Services.CommonFunctions;

namespace RegexBot;

partial class RegexbotClient {
    private CommonFunctionsService _svcCommonFunctions;

    public enum RemovalType { None, Ban, Kick }

    /// <summary>
    /// Attempts to ban the given user from the specified guild. It is greatly preferred to call this method
    /// instead of manually executing the equivalent method found in Discord.Net. It notifies other services
    /// that the action originated from the bot, and allows them to handle the action appropriately.
    /// </summary>
    /// <returns>A structure containing results of the ban operation.</returns>
    /// <param name="guild">The guild in which to attempt the ban.</param>
    /// <param name="source">The user, module, or service which is requesting this action to be taken.</param>
    /// <param name="targetUser">The user which to perform the action to.</param>
    /// <param name="purgeDays">Number of days of prior post history to delete on ban. Must be between 0-7.</param>
    /// <param name="reason">Reason for the action. Sent to the Audit Log and user (if specified).</param>
    /// <param name="sendDMToTarget">Specify whether to send a direct message to the target user informing them of the action.</param>
    public Task<BanKickResult> BanAsync(SocketGuild guild, string source, ulong targetUser,
                                        int purgeDays, string reason, bool sendDMToTarget)
        => _svcCommonFunctions.BanOrKickAsync(RemovalType.Ban, guild, source, targetUser, purgeDays, reason, sendDMToTarget);

    /// <summary>
    /// Similar to <see cref="BanAsync(SocketGuild, string, ulong, int, string, bool)"/>, but making use of an
    /// EntityCache lookup to determine the target.
    /// </summary>
    /// <param name="targetSearch">The EntityCache search string.</param>
    public async Task<BanKickResult> BanAsync(SocketGuild guild, string source, string targetSearch,
                                              int purgeDays, string reason, bool sendDMToTarget) {
        var result = await EcQueryUser(guild.Id, targetSearch);
        if (result == null) return new BanKickResult(null, false, true, RemovalType.Ban, 0);
        return await BanAsync(guild, source, result.UserID, purgeDays, reason, sendDMToTarget);
    }

    /// <summary>
    /// Attempts to ban the given user from the specified guild. It is greatly preferred to call this method
    /// instead of manually executing the equivalent method found in Discord.Net. It notifies other services
    /// that the action originated from the bot, and allows them to handle the action appropriately.
    /// </summary>
    /// <returns>A structure containing results of the ban operation.</returns>
    /// <param name="guild">The guild in which to attempt the kick.</param>
    /// <param name="source">The user, module, or service which is requesting this action to be taken.</param>
    /// <param name="targetUser">The user which to perform the action to.</param>
    /// <param name="reason">Reason for the action. Sent to the Audit Log and user (if specified).</param>
    /// <param name="sendDMToTarget">Specify whether to send a direct message to the target user informing them of the action being taken.</param>
    public Task<BanKickResult> KickAsync(SocketGuild guild, string source, ulong targetUser, string reason, bool sendDMToTarget)
        => _svcCommonFunctions.BanOrKickAsync(RemovalType.Ban, guild, source, targetUser, 0, reason, sendDMToTarget);

    /// <summary>
    /// Similar to <see cref="KickAsync(SocketGuild, string, ulong, string, bool)"/>, but making use of an
    /// EntityCache lookup to determine the target.
    /// </summary>
    /// <param name="targetSearch">The EntityCache search string.</param>
    public async Task<BanKickResult> KickAsync(SocketGuild guild, string source, string targetSearch, string reason, bool sendDMToTarget) {
        var result = await EcQueryUser(guild.Id, targetSearch);
        if (result == null) return new BanKickResult(null, false, true, RemovalType.Kick, 0);
        return await KickAsync(guild, source, result.UserID, reason, sendDMToTarget);
    }
}
