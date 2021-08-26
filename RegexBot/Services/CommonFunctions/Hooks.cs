using Discord.WebSocket;
using System.Threading.Tasks;
using RegexBot.Services.CommonFunctions;

namespace RegexBot
{
    partial class RegexbotClient
    {
        private CommonFunctionsService _svcCommonFunctions;

        public enum RemovalType { None, Ban, Kick }

        /// <summary>
        /// See <see cref="ModuleBase.BanAsync(SocketGuild, string, ulong, int, string, string)"/>
        /// and related methods.
        /// </summary>
        public Task<BanKickResult> BanOrKickAsync(RemovalType t, SocketGuild guild, string source,
            ulong target, int banPurgeDays, string logReason, bool sendDMToTarget)
            => _svcCommonFunctions.BanOrKickAsync(t, guild, source, target, banPurgeDays, logReason, sendDMToTarget);
    }
}
