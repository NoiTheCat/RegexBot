using CommandLine;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace RegexBot;
class Configuration {
    /// <summary>
    /// Token used for Discord authentication.
    /// </summary>
    internal string BotToken { get; }

    /// <summary>
    /// List of assemblies to load, by file. Paths are always relative to the bot directory.
    /// </summary>
    internal IReadOnlyList<string> Assemblies { get; }

    public JObject ServerConfigs { get; }

    // SQL properties:
    public string? Host { get; }
    public string? Database { get; }
    public string Username { get; }
    public string Password { get; }

    /// <summary>
    /// Sets up instance configuration object from file and command line parameters.
    /// </summary>
    internal Configuration() {
        var args = CommandLineParameters.Parse(Environment.GetCommandLineArgs());
        var path = args?.ConfigFile!;

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

        var dbconf = conf["DatabaseOptions"]?.Value<JObject>();
        if (dbconf == null) throw new Exception("Database settings were not specified in configuration.");
        // TODO more detailed database configuration? password file, other advanced authentication settings... look into this.
        Host = ReadConfKey<string>(dbconf, nameof(Host), false);
        Database = ReadConfKey<string?>(dbconf, nameof(Database), false);
        Username = ReadConfKey<string>(dbconf, nameof(Username), true);
        Password = ReadConfKey<string>(dbconf, nameof(Password), true);

        ServerConfigs = conf["Servers"]?.Value<JObject>();
        if (ServerConfigs == null) throw new Exception("No server configurations were specified.");
    }

    private static T? ReadConfKey<T>(JObject jc, string key, [DoesNotReturnIf(true)] bool failOnEmpty) {
        if (jc.ContainsKey(key)) return jc[key]!.Value<T>();
        if (failOnEmpty) throw new Exception($"'{key}' must be specified in the instance configuration.");
        return default;
    }

    class CommandLineParameters {
        [Option('c', "config", Default = "config.json")]
        public string? ConfigFile { get; set; } = null;

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
