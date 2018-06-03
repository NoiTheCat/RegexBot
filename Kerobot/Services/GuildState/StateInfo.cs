using Newtonsoft.Json.Linq;
using System;

namespace Kerobot.Services.GuildState
{
    /// <summary>
    /// Contains the guild state object and other useful metadata in regards to it.
    /// </summary>
    class StateInfo : IDisposable
    {
        static readonly TimeSpan TimeUntilStale = new TimeSpan(0, 15, 0);

        private readonly object _data;
        /// <summary>
        /// Module-provided data.
        /// </summary>
        public object Data => _data;

        /// <summary>
        /// Hash of the JToken used to generate the data. In certain casaes, it is used to check
        /// if the configuration may be stale and needs to be reloaded.
        /// </summary>
        private readonly int _configHash;

        private readonly DateTimeOffset _creationTs;

        public StateInfo(object data, int configHash)
        {
            _data = data;
            _configHash = configHash;
            _creationTs = DateTimeOffset.UtcNow;
        }

        public void Dispose()
        {
            if (_data is IDisposable dd) { dd.Dispose(); }
        }

        /// <summary>
        /// Checks if the current data may be stale, based on the data's age or
        /// through comparison with incoming configuration.
        /// </summary>
        public bool IsStale(JToken comparison)
        {
            if (DateTimeOffset.UtcNow - _creationTs > TimeUntilStale) return true;
            if (comparison.GetHashCode() != _configHash) return true;
            return false;
        }
    }
}
