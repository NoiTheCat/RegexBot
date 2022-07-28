using CommandLine;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
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
    internal IReadOnlyList<string> Assemblies { get; }

    public string? SqlHost { get; }
    public string? SqlDatabase { get; }
    public string SqlUsername { get; }
    public string SqlPassword { get; }

    /// <summary>
    /// Sets up instance configuration object from file and command line parameters.
    /// </summary>
    internal InstanceConfig() {
        var args = CommandLineParameters.Parse(Environment.GetCommandLineArgs());
        var path = args?.ConfigFile ?? Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)
            + Path.DirectorySeparatorChar + "." + Path.DirectorySeparatorChar + "instance.json";

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

        BotToken = ReadConfKey<string>(conf, nameof(BotToken), true);

        try {
            Assemblies = Common.Utilities.LoadStringOrStringArray(conf[nameof(Assemblies)]).AsReadOnly();
        } catch (ArgumentNullException) {
            Assemblies = Array.Empty<string>();
        } catch (ArgumentException) {
            throw new Exception($"'{nameof(Assemblies)}' is not properly specified in configuration.");
        }

        SqlHost = ReadConfKey<string>(conf, nameof(SqlHost), false);
        SqlDatabase = ReadConfKey<string?>(conf, nameof(SqlDatabase), false);
        SqlUsername = ReadConfKey<string>(conf, nameof(SqlUsername), true);
        SqlPassword = ReadConfKey<string>(conf, nameof(SqlPassword), true);
    }

    private static T? ReadConfKey<T>(JObject jc, string key, [DoesNotReturnIf(true)] bool failOnEmpty) {
        if (jc.ContainsKey(key)) return jc[key]!.Value<T>();
        if (failOnEmpty) throw new Exception($"'{key}' must be specified in the instance configuration.");
        return default;
    }

    /// <summary>
    /// Command line options
    /// </summary>
    class CommandLineParameters {
        [Option('c', "config", Default = null,
            HelpText = "Custom path to instance configuration. Defaults to instance.json in bot directory.")]
        public string ConfigFile { get; set; } = null!;

        /// <summary>
        /// Command line arguments parsed here. Depending on inputs, the program can exit here.
        /// </summary>
        public static CommandLineParameters? Parse(string[] args) {
            CommandLineParameters? result = null;

            new Parser(settings => {
                settings.IgnoreUnknownArguments = true;
                settings.AutoHelp = false;
                settings.AutoVersion = false;
            }).ParseArguments<CommandLineParameters>(args)
                .WithParsed(p => result = p)
                .WithNotParsed(e => { /* ignore */ });
            return result;
        }
    }
}
