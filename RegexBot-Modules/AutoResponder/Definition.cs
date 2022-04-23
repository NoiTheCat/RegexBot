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
    public IReadOnlyList<string> Response { get; }
    public string? Command { get; }
    public FilterList Filter { get; }
    public RateLimit<ulong> RateLimit { get; }
    public double RandomChance { get; }

    /// <summary>
    /// Creates an instance based on JSON configuration.
    /// </summary>
    public Definition(JProperty incoming) {
        Label = incoming.Name;
        if (incoming.Value.Type != JTokenType.Object)
            throw new ModuleLoadException($"Value of {nameof(AutoResponder)} definition must be a JSON object.");
        var data = (JObject)incoming.Value;

        // error message postfix
        var errpofx = $" in AutoRespond definition '{Label}'.";

        // Parse regex
        const RegexOptions rxopts = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline;
        var regexes = new List<Regex>();
        var rxconf = data[nameof(Regex)];
        // Accepting either array or single string
        // TODO this code could be moved into a helper method somehow
        if (rxconf?.Type == JTokenType.Array) {
            foreach (var input in rxconf.Values<string>()) {
                try {
                    var r = new Regex(input!, rxopts);
                    regexes.Add(r);
                } catch (ArgumentException) {
                    throw new ModuleLoadException($"Failed to parse regular expression pattern '{input}'{errpofx}");
                }
            }
        } else if (rxconf?.Type == JTokenType.String) {
            var rxstr = rxconf.Value<string>()!;
            try {
                var r = new Regex(rxstr, rxopts);
                regexes.Add(r);
            } catch (ArgumentException) {
                throw new ModuleLoadException($"Failed to parse regular expression pattern '{rxstr}'{errpofx}");
            }
        } else {
            throw new ModuleLoadException("'Regex' not defined" + errpofx);
        }
        Regex = regexes.AsReadOnly();

        bool responseDefined;

        // Get response
        // TODO this bit could also go into the same aforementioned helper method
        var replyconf = data["reply"];
        if (replyconf?.Type == JTokenType.String) {
            var str = replyconf.Value<string>()!;
            Response = new List<string>() { str }.AsReadOnly();
            responseDefined = true;
        } else if (replyconf?.Type == JTokenType.Array) {
            Response = new List<string>(replyconf.Values<string>()!).AsReadOnly();
            responseDefined = true;
        } else {
            Response = Array.Empty<string>();
            responseDefined = false;
        }
        // Get command
        var commconf = data[nameof(Command)];
        if (commconf != null && responseDefined) {
            throw new ModuleLoadException("Cannot have 'Response' and 'Command' defined at the same time" + errpofx);
        }
        if (!responseDefined) {
            if (commconf != null) {
                var commstr = commconf.Value<string>();
                if (string.IsNullOrWhiteSpace(commstr))
                    throw new ModuleLoadException("'Command' is defined, but value is blank" + errpofx);
                Command = commstr;
                responseDefined = true;
            }
        }
        // Got neither...
        if (!responseDefined) throw new ModuleLoadException("Neither 'Response' nor 'Command' were defined" + errpofx);

        // Filtering
        Filter = new FilterList(data);

        // Rate limiting
        var rlconf = data[nameof(RateLimit)];
        if (rlconf?.Type == JTokenType.Integer) {
            var rlval = rlconf.Value<uint>();
            RateLimit = new RateLimit<ulong>(rlval);
        } else if (rlconf != null) {
            throw new ModuleLoadException("'RateLimit' must be a non-negative integer" + errpofx);
        } else {
            RateLimit = new(0);
        }
        var rlstr = data[nameof(RateLimit)]?.Value<ushort>();

        // Random chance parameter
        var randstr = data[nameof(RandomChance)]?.Value<string>();
        double randval;
        if (string.IsNullOrWhiteSpace(randstr)) {
            randval = double.NaN;
        } else {
            if (!double.TryParse(randstr, out randval)) {
                throw new ModuleLoadException("Random value is invalid (unable to parse)" + errpofx);
            }
            if (randval is > 1 or < 0) {
                throw new ModuleLoadException("Random value is invalid (not between 0 and 1)" + errpofx);
            }
        }
        RandomChance = randval;
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
        bool matchFound = false;
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
        if (Response.Count == 1) return Response[0];
        return Response[Chance.Next(0, Response.Count - 1)];
    }
}
