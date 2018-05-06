using System;
using System.Collections.Generic;
using System.Text;

namespace Kerobot.Services
{
    /// <summary>
    /// Base class for Kerobot service.
    /// </summary>
    /// <remarks>
    /// Services provide the core functionality of this program. Modules are expected to call into methods
    /// provided by services for the times when processor-intensive or shared functionality needs to be utilized.
    /// </remarks>
    internal class Service
    {
        private readonly Kerobot _kb;

        public Kerobot Kerobot => _kb;

        protected internal Service(Kerobot kb)
        {
            _kb = kb;
        }
    }
}
