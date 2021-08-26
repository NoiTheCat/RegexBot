using Newtonsoft.Json.Linq;
using System;

namespace RegexBot.Services.GuildState
{
    /// <summary>
    /// Contains a guild state object and other useful metadata in regards to it.
    /// </summary>
    class StateInfo : IDisposable
    {
        static readonly TimeSpan TimeUntilStale = new TimeSpan(0, 15, 0);
        
        /// <summary>
        /// Module-provided data.
        /// </summary>
        public object Data { get; }

        /// <summary>
        /// Hash of the JToken used to generate the data. In certain casaes, it is used to check
        /// if the configuration may be stale and needs to be reloaded.
        /// </summary>
        private readonly int _configHash;
        
        private DateTimeOffset _lastStaleCheck;

        public StateInfo(object data, int configHash)
        {
            Data = data;
            _configHash = configHash;
            _lastStaleCheck = DateTimeOffset.UtcNow;
        }

        public void Dispose()
        {
            if (Data is IDisposable dd) { dd.Dispose(); }
        }

        /// <summary>
        /// Checks if the current data may be stale, based on the last staleness check or
        /// if the underlying configuration has changed.
        /// </summary>
        public bool IsStale(JToken comparison)
        {
            if (DateTimeOffset.UtcNow - _lastStaleCheck > TimeUntilStale) return true;
            if (comparison.GetHashCode() != _configHash) return true;
            _lastStaleCheck = DateTimeOffset.UtcNow;
            return false;
        }
    }
}
