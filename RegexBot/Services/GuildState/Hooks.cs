using RegexBot.Common;
using RegexBot.Services.GuildState;
using System;

namespace RegexBot
{
    partial class RegexbotClient
    {
        private GuildStateService _svcGuildState;

        /// <summary>
        /// See <see cref="ModuleBase.GetGuildState{T}(ulong)"/>.
        /// </summary>
        internal T GetGuildState<T>(ulong guild, Type type)
            => _svcGuildState.RetrieveGuildStateObject<T>(guild, type);

        /// <summary>
        /// See <see cref="ModuleBase.GetModerators(ulong)"/>.
        /// </summary>
        internal EntityList GetModerators(ulong guild) => _svcGuildState.RetrieveGuildModerators(guild);
    }
}
