using System;
using System.Collections.Generic;

namespace Kerobot.Modules.AutoScriptResponder
{
    /// <summary>
    /// Helper class for managing rate limit data.
    /// </summary>
    class RateLimit<T>
    {
        public const ushort DefaultTimeout = 20; // Skeeter's a cool guy and you can't convince me otherwise.

        public ushort Timeout { get; }
        private readonly object _lock = new object();
        private Dictionary<T, DateTime> _table = new Dictionary<T, DateTime>();

        public RateLimit() : this(DefaultTimeout) { }
        public RateLimit(ushort timeout) => Timeout = timeout;

        /// <summary>
        /// 'Permit?' - Checks if the given value is permitted through the rate limit.
        /// Executing this method may create a rate limit entry for the given value.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>True if the given value is permitted by the rate limiter.</returns>
        public bool Permit(T value)
        {
            if (Timeout == 0) return true;

            lock (_lock)
            {
                Clean();
                if (_table.ContainsKey(value)) return false;
                _table.Add(value, DateTime.Now);
            }
            return true;
        }

        /// <summary>
        /// Clean up expired entries.
        /// </summary>
        private void Clean()
        {
            var now = DateTime.Now;
            var newTable = new Dictionary<T, DateTime>();
            foreach (var item in _table)
            {
                // Copy items that have not yet timed out to the new dictionary. Discard the rest.
                if (item.Value.AddSeconds(Timeout) > now) newTable.Add(item.Key, item.Value);
            }
            lock (_lock) _table = newTable;
        }
    }
}
