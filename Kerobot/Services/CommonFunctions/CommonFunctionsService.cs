using System.Threading.Tasks;
using Discord.Net;
using Discord.WebSocket;
using static Kerobot.Kerobot;

namespace Kerobot.Services.CommonFunctions
{
    /// <summary>
    /// Implements certain common actions that modules may want to perform. Using this service to perform those
    /// functions may help enforce a sense of consistency across modules when performing common actions, and may
    /// inform services which provide any additional features the ability to respond to those actions ahead of time.
    /// 
    /// This is currently an experimental section. If it turns out to not be necessary, this service will be removed and
    /// modules may resume executing common actions on their own.
    /// </summary>
    internal class CommonFunctionsService : Service
    {
        public CommonFunctionsService(Kerobot kb) : base(kb) { }

        #region Guild member removal
        /// <summary>
        /// Common processing for kicks and bans. Called by DoKickAsync and DoBanAsync.
        /// </summary>
        /// <param name="logReason">The reason to insert into the Audit Log.</param>
        /// <param name="dmTemplate">
        /// The message to send out to the target. Leave null to not perform this action.
        /// Instances of "%r" within it are replaced with <paramref name="logReason"/> and instances of "%g"
        /// are replaced with the server name.
        /// </param>
        internal async Task<BanKickResult> BanOrKickAsync(
            RemovalType t, SocketGuild guild, string source, ulong target, int banPurgeDays,
            string logReason, string dmTemplate)
        {
            if (string.IsNullOrWhiteSpace(logReason)) logReason = "Reason not specified.";
            var dmSuccess = true;

            SocketGuildUser utarget = guild.GetUser(target);
            // Can't kick without obtaining user object. Quit here.
            if (t == RemovalType.Kick && utarget == null) return new BanKickResult(null, false, true);

#if DEBUG
#warning "Services are NOT NOTIFIED" of bans/kicks during debug."
#else
            // TODO notify services here as soon as we get some who will want to listen to this
#endif

            // Send DM notification
            if (dmTemplate != null)
            {
                if (utarget != null) dmSuccess = await BanKickSendNotificationAsync(utarget, dmTemplate, logReason);
                else dmSuccess = false;
            }

            // Perform the action
            try
            {
#if DEBUG
#warning "Actual kick/ban is DISABLED during debug."
#else
                if (t == RemovalType.Ban) await guild.AddBanAsync(target, banPurgeDays);
                else await utarget.KickAsync(logReason);
#endif
            }
            catch (HttpException ex)
            {
                return new BanKickResult(ex, false, false);
            }

            return new BanKickResult(null, dmSuccess, false);
        }

        private async Task<bool> BanKickSendNotificationAsync(SocketGuildUser target, string dmTemplate, string reason)
        {
            if (dmTemplate == null) return true;

            var dch = await target.GetOrCreateDMChannelAsync();
            string output = dmTemplate.Replace("%r", reason).Replace("%s", target.Guild.Name);

            try { await dch.SendMessageAsync(output); }
            catch (HttpException) { return false; }

            return true;
        }
#endregion
    }
}
