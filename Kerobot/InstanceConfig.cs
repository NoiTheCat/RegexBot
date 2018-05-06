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

        // TODO add fields for services to be configurable: DMRelay, InstanceLog

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
                throw new Exception($"'{JBotToken}' was not properly specified in configuration.");
            _pgSqlConnectionString = conf[JPgSqlConnectionString]?.Value<string>();
            if (string.IsNullOrEmpty(_pgSqlConnectionString))
                throw new Exception($"'{JPgSqlConnectionString}' was not properly specified in configuration.");
        }
    }
}
