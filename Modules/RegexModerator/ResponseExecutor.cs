using Discord;
using RegexBot.Common;
using System.Text;

namespace RegexBot.Modules.RegexModerator;
/// <summary>
/// Transient helper class which handles response interpreting and execution.
/// </summary>
class ResponseExecutor {
    private const string ErrParamNeedNone = "This response type does not accept parameters.";
    private const string ErrParamWrongAmount = "Incorrect number of parameters defined in the response.";
    private const string ErrMissingUser = "The target user is no longer in the server.";

    delegate Task<ResponseResult> ResponseHandler(string? parameter);

    private readonly ConfDefinition _rule;
    private readonly RegexbotClient _bot;

    private readonly SocketGuild _guild;
    private readonly SocketGuildUser _user;
    private readonly SocketMessage _msg;

    private readonly List<(string, ResponseResult)> _reports;
    private Action<string> Log { get; }

    private string LogSource => $"{_rule.Label} ({nameof(RegexModerator)})";

    public ResponseExecutor(ConfDefinition rule, RegexbotClient bot, SocketMessage msg, Action<string> logger) {
        _rule = rule;
        _bot = bot;

        _msg = msg;
        _user = (SocketGuildUser)msg.Author;
        _guild = _user.Guild;

        _reports = new();
        Log = logger;
    }

    public async Task Execute() {
        var reportTarget = _rule.ReportingChannel?.FindChannelIn(_guild, true);
        if (_rule.ReportingChannel != null && reportTarget == null)
            Log("Could not find target reporting channel.");

        foreach (var line in _rule.Response) {
            var item = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries & StringSplitOptions.TrimEntries);
            var cmd = item[0];
            var param = item.Length >= 2 ? item[1] : null;
            ResponseHandler runLine = cmd.ToLowerInvariant() switch {
                "comment"   => CmdComment,
                "rem"       => CmdComment,
                "#"         => CmdComment,
                "ban"       => CmdBan,
                "delete"    => CmdDelete,
                "remove"    => CmdDelete,
                "kick"      => CmdKick,
                "note"      => CmdNote,
                "roleadd"   => CmdRoleAdd,
                "addrole"   => CmdRoleAdd,
                "roledel"   => CmdRoleDel,
                "delrole"   => CmdRoleDel,
                "say"       => CmdSay,
                "send"      => CmdSay,
                "reply"     => CmdSay,
                "timeout"   => CmdTimeout,
                "mute"      => CmdTimeout,
                "warn"      => CmdWarn,
                _ => delegate (string? p) { return Task.FromResult(FromError($"Unknown command '{cmd}'.")); }
            };
            
            try {
                var result = await runLine(param);
                _reports.Add((cmd, result));
            } catch (Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Forbidden) {
                _reports.Add((cmd, FromError(Messages.ForbiddenGenericError)));
            }
        }

        // Handle reporting
        if (reportTarget != null) {
            // Set up report
            var rptOutput = new StringBuilder();
            foreach (var (action, result) in _reports) {
                rptOutput.Append(result.Success ? ":white_check_mark:" : ":x:");
                rptOutput.Append($" `{action}`");
                if (result.LogLine != null) {
                    rptOutput.Append(": ");
                    rptOutput.Append(result.LogLine);
                }
                rptOutput.AppendLine();
            }

            // We can only afford to show a preview of the message being reported, due to embeds
            // being constrained to the same 2000 character limit as normal messages.
            const string TruncateWarning = "**Notice: Full message has been truncated.**\n";
            const int TruncateMaxLength = 990;
            var invokingLine = _msg.Content;
            if (invokingLine.Length > TruncateMaxLength) {
                invokingLine = string.Concat(TruncateWarning, invokingLine.AsSpan(0, TruncateMaxLength - TruncateWarning.Length));
            }

            var resultEmbed = new EmbedBuilder()
                .WithFields(
                    new EmbedFieldBuilder() {
                        Name = "Context",
                        Value =
                            $"User: {_user.Mention} `{_user.Id}`\n" +
                            $"Channel: <#{_msg.Channel.Id}> `#{_msg.Channel.Name}`"
                    },
                    new EmbedFieldBuilder() {
                        Name = "Response status",
                        Value = rptOutput.ToString()
                    }
                )
                .WithAuthor(
                    name: $"{_msg.Author.Username}#{_msg.Author.Discriminator} said:",
                    iconUrl: _msg.Author.GetAvatarUrl(),
                    url: _msg.GetJumpUrl()
                )
                .WithDescription(invokingLine)
                .WithFooter(
                    text: LogSource,
                    iconUrl: _bot.DiscordClient.CurrentUser.GetAvatarUrl()
                )
                .WithCurrentTimestamp()
                .Build();
            try {
                await reportTarget.SendMessageAsync(embed: resultEmbed);
            } catch (Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Forbidden) {
                Log("Encountered 403 error when attempting to send report.");
            }
        }
    }

    #region Response delegates
    private static Task<ResponseResult> CmdComment(string? parameter) => Task.FromResult(FromSuccess(parameter));

    private Task<ResponseResult> CmdBan(string? parameter) => CmdBanKick(true, parameter);
    private Task<ResponseResult> CmdKick(string? parameter) => CmdBanKick(false, parameter);
    private async Task<ResponseResult> CmdBanKick(bool isBan, string? parameter) {
        BanKickResult result;
        if (isBan) {
            result = await _bot.BanAsync(_guild, LogSource, _user.Id,
                                         _rule.BanPurgeDays, parameter, _rule.NotifyUser);
        } else {
            result = await _bot.KickAsync(_guild, LogSource, _user.Id,
                                          parameter, _rule.NotifyUser);
        }
        if (result.ErrorForbidden) return FromError(Messages.ForbiddenGenericError);
        if (result.ErrorNotFound) return FromError(ErrMissingUser);
        if (_rule.NotifyChannel) await _msg.Channel.SendMessageAsync(result.GetResultString(_bot));
        return FromSuccess(result.MessageSendSuccess ? null : "Unable to send notification DM.");
    }

    private Task<ResponseResult> CmdRoleAdd(string? parameter) => CmdRoleManipulation(parameter, true);
    private Task<ResponseResult> CmdRoleDel(string? parameter) => CmdRoleManipulation(parameter, false);
    private async Task<ResponseResult> CmdRoleManipulation(string? parameter, bool add) {
        // parameters: @_, &, reason?
        if (string.IsNullOrWhiteSpace(parameter)) return FromError(ErrParamWrongAmount);
        var param = parameter.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (param.Length != 2) return FromError(ErrParamWrongAmount);
        
        // Find targets
        SocketGuildUser? tuser;
        SocketRole? trole;
        try {
            var userName = new EntityName(param[0], EntityType.User);
            if (userName.Id.HasValue) tuser = _guild.GetUser(userName.Id.Value);
            else {
                if (userName.Name == "_") tuser = _user;
                else tuser = userName.FindUserIn(_guild);
            }
            if (tuser == null) return FromError($"Unable to find user '{userName.Name}'.");
            var roleName = new EntityName(param[1], EntityType.Role);
            if (roleName.Id.HasValue) trole = _guild.GetRole(roleName.Id.Value);
            else trole = roleName.FindRoleIn(_guild);
            if (trole == null) return FromError($"Unable to find role '{roleName.Name}'.");
        } catch (ArgumentException) {
            return FromError("User or role were not correctly set in configuration.");
        }
        
        // Do action
        var rq = new RequestOptions() { AuditLogReason = LogSource };
        if (add) await tuser.AddRoleAsync(trole, rq);
        else await tuser.RemoveRoleAsync(trole, rq);
        return FromSuccess($"{(add ? "Set" : "Unset")} {trole.Mention}.");
    }

    private async Task<ResponseResult> CmdDelete(string? parameter) {
        if (!string.IsNullOrWhiteSpace(parameter)) return FromError(ErrParamNeedNone);

        try {
            await _msg.DeleteAsync(new RequestOptions { AuditLogReason = LogSource });
            return FromSuccess();
        } catch (Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.NotFound) {
            return FromError("The message had already been deleted.");
        }
    }

    private async Task<ResponseResult> CmdSay(string? parameter) {
        // parameters: [#_/@_] message
        if (string.IsNullOrWhiteSpace(parameter)) return FromError(ErrParamWrongAmount);
        var param = parameter.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (param.Length != 2) return FromError(ErrParamWrongAmount);

        // Get target
        IMessageChannel? targetCh;
        EntityName name;
        try {
            name = new EntityName(param[0]);
        } catch (ArgumentException) {
            return FromError("Reply target was not correctly set in configuration.");
        }
        bool isUser;
        if (name.Type == EntityType.Channel) {
            if (name.Name == "_") targetCh = _msg.Channel;
            else targetCh = name.FindChannelIn(_guild);
            if (targetCh == null) return FromError($"Unable to find channel '{name.Name}'.");
            isUser = false;
        } else if (name.Type == EntityType.User) {
            if (name.Name == "_") targetCh = await _msg.Author.CreateDMChannelAsync();
            else {
                var searchedUser = name.FindUserIn(_guild);
                if (searchedUser == null) return FromError($"Unable to find user '{name.Name}'.");
                targetCh = await searchedUser.CreateDMChannelAsync();
            }
            isUser = true;
        } else {
            return FromError("Channel or user were not correctly set in configuration.");
        }
        if (targetCh == null) return FromError("Could not acquire target channel.");
        await targetCh.SendMessageAsync(param[1]);
        return FromSuccess($"Sent to {(isUser ? "user DM" : $"<#{targetCh.Id}>")}.");
    }

    private async Task<ResponseResult> CmdNote(string? parameter) {
        if (string.IsNullOrWhiteSpace(parameter)) return FromError(ErrParamWrongAmount);
        var log = await _bot.AddUserNoteAsync(_guild, _user.Id, LogSource, parameter);
        return FromSuccess($"Note \\#{log.LogId} logged for {_user}.");
    }

    private async Task<ResponseResult> CmdWarn(string? parameter) {
        if (string.IsNullOrWhiteSpace(parameter)) return FromError(ErrParamWrongAmount);
        var (log, result) = await _bot.AddUserWarnAsync(_guild, _user.Id, LogSource, parameter);
        var resultMsg = $"Warning \\#{log.LogId} logged for {_user}.";
        if (result.Success) return FromSuccess(resultMsg);
        else return FromError(resultMsg + " Failed to send DM.");
    }

    private async Task<ResponseResult> CmdTimeout(string? parameter) {
        // parameters: (time in minutes) [reason]
        if (string.IsNullOrWhiteSpace(parameter)) return FromError(ErrParamWrongAmount);
        var param = parameter.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (param.Length < 1) return FromError(ErrParamWrongAmount);

        if (!int.TryParse(param[0], out var timemin)) {
            return FromError($"Couldn't parse '{param[0]}' as amount of time in minutes.");
        }
        string? reason = null;
        if (param.Length == 2) reason = param[1];

        var result = await _bot.SetTimeoutAsync(_guild, LogSource, _user,
                                                TimeSpan.FromMinutes(timemin), reason, _rule.NotifyUser);
        if (result.ErrorForbidden) return FromError(Messages.ForbiddenGenericError);
        if (result.ErrorNotFound) return FromError(ErrMissingUser);
        if (result.Error != null) return FromError(result.Error.Message);
        if (_rule.NotifyChannel) await _msg.Channel.SendMessageAsync(result.ToResultString());
        return FromSuccess(result.Success ? null : "Unable to send notification DM.");
    }
    #endregion

    #region Response reporting
    private struct ResponseResult {
        public bool Success;
        public string? LogLine;
    }

    private static ResponseResult FromSuccess(string? logLine = null) => new() { Success = true, LogLine = logLine };
    private static ResponseResult FromError(string? logLine = null) => new() { Success = false, LogLine = logLine };
    #endregion
}
