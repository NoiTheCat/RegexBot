using Discord;
using Discord.WebSocket;
using RegexBot.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static RegexBot.RegexbotClient;

namespace RegexBot.Modules.RegexModerator
{
    /// <summary>
    /// Helper class to RegexModerator that executes the appropriate actions associated with a triggered rule.
    /// </summary>
    class ResponseExecutor
    {
        private readonly ConfDefinition _rule;
        private readonly RegexbotClient _bot;
        private List<(ResponseAction, ResponseExecutionResult)> _results;

        public ResponseExecutor(ConfDefinition rule, RegexbotClient bot)
        {
            _rule = rule;
            _bot = bot;
        }

        public async Task Execute(SocketMessage msg)
        {
            var g = ((SocketGuildUser)msg.Author).Guild;
            _results = new List<(ResponseAction, ResponseExecutionResult)>();
            var tasks = new List<Task>
            {
                ExecuteAction(DoReplyToChannel, g, msg),
                ExecuteAction(DoReplyToInvokerDM, g, msg),
                ExecuteAction(DoRoleAdd, g, msg),
                ExecuteAction(DoRoleRemove, g, msg),
                ExecuteAction(DoBan, g, msg),
                ExecuteAction(DoKick, g, msg),
                ExecuteAction(DoDelete, g, msg)
                // TODO role add/remove: add persistence option
                // TODO add note to user log
                // TODO add warning to user log
            };
            await Task.WhenAll(tasks);

            // Report can only run after all previous actions have been performed.
            await ExecuteAction(DoReport, g, msg);

            // TODO pass any local error messages to guild log
        }

        #region Response actions
        /*
         * For the sake of creating reports and notifying the user of any issues,
         * every response method should have a signature that conforms to that of the
         * ResponseAction delegate defined here.
         * Methods here should attempt to handle their own expected exceptions, and leave the
         * extraordinary exceptions for the wrapper to deal with.
         * 
         * Methods may return null, but MUST only do so if they took no action (meaning, they were
         * not meant to take any action per the input configuration). Data within each
         * ResponseExecutionResult is then used to build a report (if requested) and/or place
         * error messages into the guild log.
         */
        delegate Task<ResponseExecutionResult> ResponseAction(SocketGuild g, SocketMessage msg);

        const string ForbiddenGenericError = "Failed to perform the action due to a permissions issue.";

        private Task<ResponseExecutionResult> DoBan(SocketGuild g, SocketMessage msg)
        {
            if (_rule.RemoveAction != RemovalType.Ban) return Task.FromResult<ResponseExecutionResult>(null);
            return DoBanOrKick(g, msg, _rule.RemoveAction);
        }
        private Task<ResponseExecutionResult> DoKick(SocketGuild g, SocketMessage msg)
        {
            if (_rule.RemoveAction != RemovalType.Kick) return Task.FromResult<ResponseExecutionResult>(null);
            return DoBanOrKick(g, msg, _rule.RemoveAction);
        }
        private async Task<ResponseExecutionResult> DoBanOrKick(SocketGuild g, SocketMessage msg, RemovalType t)
        {
            var result = await _bot.BanOrKickAsync(t, g, $"Rule '{_rule.Label}'",
                msg.Author.Id, _rule.BanPurgeDays, _rule.RemoveReason, _rule.RemoveNotifyTarget);

            string logAnnounce = null;
            if (_rule.RemoveAnnounce)
            {
                try
                {
                    await msg.Channel.SendMessageAsync(result.GetResultString(_bot, g.Id));
                }
                catch (Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                {
                    logAnnounce = "Could not send " + (t == RemovalType.Ban ? "ban" : "kick") + " announcement to channel "
                        + "due to a permissions issue.";
                }
            }

            if (result.ErrorForbidden)
            {
                return new ResponseExecutionResult(false, ForbiddenGenericError);
            }
            else if (result.ErrorNotFound)
            {
                return new ResponseExecutionResult(false, "The target user is no longer in the server.");
            }
            else return new ResponseExecutionResult(true, logAnnounce);
        }

        private async Task<ResponseExecutionResult> DoDelete(SocketGuild g, SocketMessage msg)
        {
            if (!_rule.DeleteMessage) return null;
            try
            {
                await msg.DeleteAsync();
                return new ResponseExecutionResult(true, null);
            }
            catch (Discord.Net.HttpException ex)
            {
                if (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return new ResponseExecutionResult(false, ForbiddenGenericError);
                }
                else if (ex.HttpCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new ResponseExecutionResult(false, "The message has already been deleted.");
                }
                else throw;
            }
        }

        private async Task<ResponseExecutionResult> DoReplyToChannel(SocketGuild g, SocketMessage msg)
        {
            if (string.IsNullOrWhiteSpace(_rule.ReplyInChannel)) return null;
            try
            {
                await msg.Channel.SendMessageAsync(_rule.ReplyInChannel);
                return new ResponseExecutionResult(true, null);
            }
            catch (Discord.Net.HttpException ex)
            {
                if (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return new ResponseExecutionResult(false, ForbiddenGenericError);
                }
                else throw;
            }
        }

        private async Task<ResponseExecutionResult> DoReplyToInvokerDM(SocketGuild g, SocketMessage msg)
        {
            if (string.IsNullOrWhiteSpace(_rule.ReplyInDM)) return null;
            var target = await msg.Author.GetOrCreateDMChannelAsync(); // can this throw an exception?

            try
            {
                await target.SendMessageAsync(_rule.ReplyInDM);
                return new ResponseExecutionResult(true, null);
            }
            catch (Discord.Net.HttpException ex)
            {
                if (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return new ResponseExecutionResult(false, "The target user is not accepting DMs.");
                }
                else throw;
            }
        }

        private Task<ResponseExecutionResult> DoRoleAdd(SocketGuild g, SocketMessage msg)
            => RoleManipulationResponse(g, msg, true);
        private Task<ResponseExecutionResult> DoRoleRemove(SocketGuild g, SocketMessage msg)
            => RoleManipulationResponse(g, msg, false);
        private async Task<ResponseExecutionResult> RoleManipulationResponse(SocketGuild g, SocketMessage msg, bool add)
        {
            EntityName ck;
            if (add)
            {
                if (_rule.RoleAdd == null) return null;
                ck = _rule.RoleAdd;
            }
            else
            {
                if (_rule.RoleRemove == null) return null;
                ck = _rule.RoleRemove;
            }

            SocketRole target = ck.FindRoleIn(g, false);
            if (target == null)
            {
                return new ResponseExecutionResult(false,
                    $"Unable to determine the role to be {(add ? "added" : "removed")}. Does it still exist?");
            }

            try
            {
                if (add) await ((SocketGuildUser)msg.Author).AddRoleAsync(target);
                else await ((SocketGuildUser)msg.Author).RemoveRoleAsync(target);

                return new ResponseExecutionResult(true, null);
            }
            catch (Discord.Net.HttpException ex)
            {
                if (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return new ResponseExecutionResult(false, ForbiddenGenericError);
                }
                else throw;
            }
        }
        #endregion

        #region Reporting
        private class ResponseExecutionResult
        {
            public bool Success { get; }
            public string Notice { get; }

            public ResponseExecutionResult(bool success, string log)
            {
                Success = success;
                Notice = log;
            }
        }

        private async Task ExecuteAction(ResponseAction action, SocketGuild g, SocketMessage arg)
        {
            ResponseExecutionResult result;
            try { result = await action(g, arg); }
            catch (Exception ex)
            {
                result = new ResponseExecutionResult(false,
                    "An unknown error occurred. The bot maintainer has been notified.");
                await _bot.InstanceLogAsync(true, nameof(RegexModerator),
                    "An unexpected error occurred while executing a response. "
                    + $"Guild: {g.Id} - Rule: '{_rule.Label}' - Exception detail:\n"
                    + ex.ToString());
            }

            if (result != null)
            {
                lock (_results) _results.Add((action, result));
            }
        }

        private async Task<ResponseExecutionResult> DoReport(SocketGuild g, SocketMessage msg)
        {
            if (_rule.ReportingChannel == null) return null;

            // Determine channel before anything else
            var ch = _rule.ReportingChannel.FindChannelIn(g, true);
            if (ch == null) return new ResponseExecutionResult(false, "Unable to find reporting channel.");

            var rptOutput = new StringBuilder();
            foreach (var (action, result) in _results) // Locking of _results not necessary at this point
            {
                if (result == null) continue;
                rptOutput.Append(result.Success ? ":white_check_mark:" : ":x:");
                rptOutput.Append(" " + action.Method.Name);
                if (result.Notice != null)
                    rptOutput.Append(" - " + result.Notice);
                rptOutput.AppendLine();
            }
            // Report status goes last. It is presumed to succeed. If it fails, the message won't make it anyway.
            rptOutput.Append($":white_check_mark: {nameof(DoReport)}");

            // We can only afford to show a preview of the message being reported, due to embeds
            // being constrained to the same 2000 character limit.
            const string TruncateWarning = "**Notice: Full message has been truncated.**\n";
            const int TruncateMaxLength = 990;
            var invokingLine = msg.Content;
            if (invokingLine.Length > TruncateMaxLength)
            {
                invokingLine = TruncateWarning + invokingLine.Substring(0, TruncateMaxLength - TruncateWarning.Length);
            }

            var resultEm = new EmbedBuilder()
            {
                Color = new Color(0xEDCE00), // TODO configurable later?

                Author = new EmbedAuthorBuilder()
                {
                    Name = $"{msg.Author.Username}#{msg.Author.Discriminator} said:",
                    IconUrl = msg.Author.GetAvatarUrl()
                },
                Description = invokingLine,

                Footer = new EmbedFooterBuilder() { Text = $"Rule: {_rule.Label}" },
                Timestamp = msg.EditedTimestamp ?? msg.Timestamp
            }.AddField(new EmbedFieldBuilder()
            {
                Name = "Actions taken:",
                Value = rptOutput.ToString()
            }).Build();

            try
            {
                await ch.SendMessageAsync(embed: resultEm);
                return new ResponseExecutionResult(true, null);
            }
            catch (Discord.Net.HttpException ex)
            {
                if (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return new ResponseExecutionResult(false, ForbiddenGenericError);
                }
                else throw;
            }
        }
        #endregion
    }
}
