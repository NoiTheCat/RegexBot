using System.Threading.Tasks;

namespace Kerobot.Services
{
    /// <summary>
    /// Base class for Kerobot service.
    /// </summary>
    /// <remarks>
    /// Services provide the core functionality of this program. Modules are expected to call into methods
    /// provided by services for the times when processor-intensive or shared functionality needs to be utilized.
    /// </remarks>
    internal abstract class Service
    {
        private readonly Kerobot _kb;
        public Kerobot Kerobot => _kb;

        public string Name => this.GetType().Name;

        public Service(Kerobot kb)
        {
            _kb = kb;
        }

        /// <summary>
        /// Initializes database tables per-guild. Called when entering a guild.
        /// </summary>
        /// <param name="db">An opened database connection with the appropriate schema option set.</param>
        /// <remarks>If overriding, calling the base method is not necessary.</remarks>
        public virtual Task CreateDatabaseTablesAsync(Npgsql.NpgsqlConnection db) => Task.CompletedTask;

        /// <summary>
        /// Creates a log message.
        /// </summary>
        /// <param name="message">Logging message contents.</param>
        /// <param name="report">Determines if the log message should be sent to a reporting channel.</param>
        /// <returns></returns>
        protected Task Log(string message, bool report = false) => Kerobot.InstanceLogAsync(report, Name, message);
    }
}
