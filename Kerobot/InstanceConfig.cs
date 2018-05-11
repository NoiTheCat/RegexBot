using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Reflection;

namespace Kerobot
{
    /// <summary>
    /// Contains instance configuration for this bot,
    /// including Discord connection settings and service configuration.
    /// </summary>
    class InstanceConfig
    {
        const string JBotToken = "BotToken";
        readonly string _botToken;
        /// <summary>
        /// Token used for Discord authentication.
        /// </summary>
        internal string BotToken => _botToken;

        const string JPgSqlConnectionString = "SqlConnectionString";
        readonly string _pgSqlConnectionString;
        /// <summary>
        /// Connection string for accessing the PostgreSQL database.
        /// </summary>
        /// <remarks>
        /// That's right, the user can specify the -entire- thing.
        /// Should problems arise, this will be replaced by a full section within configuration.
        /// </remarks>
        internal string PostgresConnString => _pgSqlConnectionString;

        const string JInstanceLogReportTarget = "LogTarget";
        readonly ulong _ilReptGuild, _ilReptChannel;
        /// <summary>
        /// Guild and channel ID, respectively, for instance log reporting.
        /// Specified as "(guild ID)/(channel ID)".
        /// </summary>
        internal (ulong, ulong) InstanceLogReportTarget => (_ilReptGuild, _ilReptChannel);

        // TODO add fields for services to be configurable: DMRelay

        /// <summary>
        /// Sets up instance configuration object from file and command line parameters.
        /// </summary>
        /// <param name="path">Path to file from which to load configuration. If null, uses default path.</param>
        internal InstanceConfig(Options options)
        {
            string path = options.ConfigFile;
            if (path == null) // default: config.json in working directory
            {
                path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                    + "." + Path.DirectorySeparatorChar + "config.json";
            }

            JObject conf;
            try
            {
                var conftxt = File.ReadAllText(path);
                conf = JObject.Parse(conftxt);
            }
            catch (Exception ex)
            {
                string pfx;
                if (ex is JsonException) pfx = "Unable to parse configuration: ";
                else pfx = "Unable to access configuration: ";

                throw new Exception(pfx + ex.Message, ex);
            }

            // Input validation - throw exception on errors. Exception messages printed as-is.
            _botToken = conf[JBotToken]?.Value<string>();
            if (string.IsNullOrEmpty(_botToken))
                throw new Exception($"'{JBotToken}' is not properly specified in configuration.");
            _pgSqlConnectionString = conf[JPgSqlConnectionString]?.Value<string>();
            if (string.IsNullOrEmpty(_pgSqlConnectionString))
                throw new Exception($"'{JPgSqlConnectionString}' is not properly specified in configuration.");

            var ilInput = conf[JInstanceLogReportTarget]?.Value<string>();
            if (!string.IsNullOrWhiteSpace(ilInput))
            {
                int idx = ilInput.IndexOf('/');
                if (idx < 0) throw new Exception($"'{JInstanceLogReportTarget}' is not properly specified in configuration.");
                try
                {
                    _ilReptGuild = ulong.Parse(ilInput.Substring(0, idx));
                    _ilReptChannel = ulong.Parse(ilInput.Substring(idx + 1, ilInput.Length - (idx + 1)));
                }
                catch (FormatException)
                {
                    throw new Exception($"'{JInstanceLogReportTarget}' is not properly specified in configuration.");
                }
            }
            else
            {
                // Feature is disabled
                _ilReptGuild = 0;
                _ilReptChannel = 0;
            }
        }
    }
}
