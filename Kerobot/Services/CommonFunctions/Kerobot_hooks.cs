using Discord.WebSocket;
using System.Threading.Tasks;
using Kerobot.Services.CommonFunctions;

namespace Kerobot
{
    partial class Kerobot
    {
        private CommonFunctionsService _svcCommonFunctions;

        public enum RemovalType { Ban, Kick }

        /// <summary>
        /// See <see cref="ModuleBase.BanAsync(SocketGuild, string, ulong, int, string, string)"/>
        /// and related methods.
        /// </summary>
        public Task<BanKickResult> BanOrKickAsync(RemovalType t, SocketGuild guild, string source,
            ulong target, int banPurgeDays, string logReason, string dmTemplate)
            => _svcCommonFunctions.BanOrKickAsync(t, guild, source, target, banPurgeDays, logReason, dmTemplate);
    }
}
