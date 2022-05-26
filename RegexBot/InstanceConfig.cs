using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RegexBot.Data;
using System.Reflection;

namespace RegexBot;

/// <summary>
/// Contains essential instance configuration for this bot including Discord connection settings, service configuration,
/// and command-line options.
/// </summary>
class InstanceConfig {
    /// <summary>
    /// Token used for Discord authentication.
    /// </summary>
    internal string BotToken { get; }

    /// <summary>
    /// List of assemblies to load, by file. Paths are always relative to the bot directory.
    /// </summary>
    internal string[] Assemblies { get; }

    /// <summary>
    /// Webhook URL for bot log reporting.
    /// </summary>
    internal string InstanceLogTarget { get; }

    // TODO add fields for services to be configurable: DMRelay

    /// <summary>
    /// Sets up instance configuration object from file and command line parameters.
    /// </summary>
    internal InstanceConfig(string[] cmdline) {
        var opts = Options.ParseOptions(cmdline);

        var path = opts.ConfigFile;
        if (path == null) { // default: config.json in working directory
            path = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)
                + "." + Path.DirectorySeparatorChar + "config.json";
        }

        JObject conf;
        try {
            var conftxt = File.ReadAllText(path);
            conf = JObject.Parse(conftxt);
        } catch (Exception ex) {
            string pfx;
            if (ex is JsonException) pfx = "Unable to parse configuration: ";
            else pfx = "Unable to access configuration: ";

            throw new Exception(pfx + ex.Message, ex);
        }

        // Input validation - throw exception on errors. Exception messages printed as-is.
        BotToken = conf[nameof(BotToken)]?.Value<string>()!;
        if (string.IsNullOrEmpty(BotToken))
            throw new Exception($"'{nameof(BotToken)}' is not properly specified in configuration.");

        var pginput = conf[nameof(BotDatabaseContext.PostgresConnectionString)]?.Value<string>()!;
        if (string.IsNullOrEmpty(pginput))
            throw new Exception($"'{nameof(BotDatabaseContext.PostgresConnectionString)}' is not properly specified in configuration.");
        BotDatabaseContext.PostgresConnectionString = pginput;

        InstanceLogTarget = conf[nameof(InstanceLogTarget)]?.Value<string>()!;
        if (string.IsNullOrEmpty(InstanceLogTarget))
            throw new Exception($"'{nameof(InstanceLogTarget)}' is not properly specified in configuration.");

        var asmList = conf[nameof(Assemblies)];
        if (asmList == null || asmList.Type != JTokenType.Array) {
            throw new Exception($"'{nameof(Assemblies)}' is not properly specified in configuration.");
        }
        var asmListImport = new List<string>();
        foreach (var line in asmList.Values<string>()) if (!string.IsNullOrEmpty(line)) asmListImport.Add(line);
        Assemblies = asmListImport.ToArray();
    }
}
