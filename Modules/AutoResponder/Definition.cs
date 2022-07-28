using RegexBot.Common;
using System.Text.RegularExpressions;

namespace RegexBot.Modules.AutoResponder;
/// <summary>
/// Representation of a single <see cref="AutoResponder"/> configuration definition.
/// </summary>
class Definition {
    private static readonly Random Chance = new();

    public string Label { get; }
    public IEnumerable<Regex> Regex { get; }
    public IReadOnlyList<string> Reply { get; }
    public string? Command { get; }
    public FilterList Filter { get; }
    public RateLimit<ulong> RateLimit { get; }
    public double RandomChance { get; }

    /// <summary>
    /// Creates an instance based on JSON configuration.
    /// </summary>
    public Definition(JObject def) {
        Label = def[nameof(Label)]?.Value<string>()
            ?? throw new ModuleLoadException($"Encountered a rule without a defined {nameof(Label)}.");

        var errpostfx = $" in the rule definition for '{Label}'.";

        // Regex
        var opts = RegexOptions.Compiled | RegexOptions.CultureInvariant;
        // Reminder: in Singleline mode, all contents are subject to the same regex (useful if e.g. spammer separates words line by line)
        opts |= RegexOptions.Singleline;
        // IgnoreCase is enabled by default; must be explicitly set to false
        if (def["IgnoreCase"]?.Value<bool>() ?? true) opts |= RegexOptions.IgnoreCase;

        const string ErrNoRegex = $"No patterns were defined under {nameof(Regex)}";
        var regexRules = new List<Regex>();
        List<string> inputs;
        try {
            inputs = Utilities.LoadStringOrStringArray(def[nameof(Regex)]);
        } catch (ArgumentNullException) {
            throw new ModuleLoadException(ErrNoRegex + errpostfx);
        }
        foreach (var inputRule in inputs) {
            try {
                regexRules.Add(new Regex(inputRule, opts));
            } catch (Exception ex) when (ex is ArgumentException or NullReferenceException) {
                throw new ModuleLoadException("Unable to parse regular expression pattern" + errpostfx);
            }
        }
        if (regexRules.Count == 0) throw new ModuleLoadException(ErrNoRegex + errpostfx);
        Regex = regexRules.AsReadOnly();

        // Filtering
        Filter = new FilterList(def);

        bool haveResponse;

        // Reply options
        var replyConf = def[nameof(Reply)];
        try {
            Reply = Utilities.LoadStringOrStringArray(replyConf);
            haveResponse = Reply.Count > 0;
        } catch (ArgumentNullException) {
            Reply = Array.Empty<string>();
            haveResponse = false;
        } catch (ArgumentException) {
            throw new ModuleLoadException($"Encountered a problem within 'Reply'{errpostfx}");
        }

        // Command options
        Command = def[nameof(Command)]?.Value<string>()!;
        if (Command != null && haveResponse)
            throw new ModuleLoadException($"Only one of either '{nameof(Reply)}' or '{nameof(Command)}' may be defined{errpostfx}");
        if (Command != null) {
            if (string.IsNullOrWhiteSpace(Command))
                throw new ModuleLoadException($"'{nameof(Command)}' must have a non-blank value{errpostfx}");
            haveResponse = true;
        }

        if (!haveResponse) throw new ModuleLoadException($"Neither '{nameof(Reply)}' nor '{nameof(Command)}' were defined{errpostfx}");

        // Rate limiting
        var rlconf = def[nameof(RateLimit)];
        if (rlconf?.Type == JTokenType.Integer) {
            var rlval = rlconf.Value<int>();
            RateLimit = new RateLimit<ulong>(rlval);
        } else if (rlconf != null) {
            throw new ModuleLoadException($"'{nameof(RateLimit)}' must be a non-negative integer{errpostfx}");
        } else {
            RateLimit = new(0);
        }

        // Random chance parameter
        var randconf = def[nameof(RandomChance)];
        if (randconf?.Type == JTokenType.Float) {
            RandomChance = randconf.Value<float>();
            if (RandomChance is > 1 or < 0) {
                throw new ModuleLoadException($"Random value is invalid (not between 0 and 1){errpostfx}");
            }
        } else if (randconf != null) {
            throw new ModuleLoadException($"{nameof(RandomChance)} is not correctly defined{errpostfx}");
        } else {
            // Default to none if undefined
            RandomChance = double.NaN;
        }
    }

    /// <summary>
    /// Checks the given message to determine if it matches this rule's constraints.
    /// This method also maintains rate limiting and performs random number generation.
    /// </summary>
    /// <returns>True if the rule's response(s) should be executed.</returns>
    public bool Match(SocketMessage m) {
        // Filter check
        if (Filter.IsFiltered(m, true)) return false;

        // Match check
        var matchFound = false;
        foreach (var item in Regex) {
            if (item.IsMatch(m.Content)) {
                matchFound = true;
                break;
            }
        }
        if (!matchFound) return false;

        // Rate limit check - currently per channel
        if (!RateLimit.IsPermitted(m.Channel.Id)) return false;

        // Random chance check
        if (!double.IsNaN(RandomChance)) {
            // Fail if randomly generated value is higher than the parameter
            // Example: To fail a 75% chance, the check value must be between 0.75000...001 and 1.0.
            var chk = Chance.NextDouble();
            if (chk > RandomChance) return false;
        }

        return true;
    }

    /// <summary>
    /// Gets a response string to display in the channel.
    /// </summary>
    public string GetResponse() {
        // TODO feature request: option to show responses in order instead of random
        if (Reply.Count == 1) return Reply[0];
        return Reply[Chance.Next(0, Reply.Count - 1)];
    }
}
