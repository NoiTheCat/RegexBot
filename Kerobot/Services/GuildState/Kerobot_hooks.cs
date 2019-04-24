using Kerobot.Common;
using Kerobot.Services.GuildState;
using System;

namespace Kerobot
{
    partial class Kerobot
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
