using Discord;
using RegexBot.Common;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace RegexBot.Modules.RegexModerator;
/// <summary>
/// Representation of a single RegexModerator rule for a guild.
/// Data in this class is immutable. Contains various helper methods.
/// </summary>
[DebuggerDisplay("RM rule '{Label}'")]
class ConfDefinition {
    public string Label { get; }

    // Matching settings
    private IEnumerable<Regex> Regex { get; }
    private FilterList Filter { get; }
    private bool IgnoreMods { get; }
    private bool ScanEmbeds { get; }

    // Response settings
    public EntityName? ReportingChannel { get; }
    public IReadOnlyList<string> Response { get; }
    public int BanPurgeDays { get; }
    public bool NotifyChannelOfRemoval { get; }
    public bool NotifyUserOfRemoval { get; }

    public ConfDefinition(JObject def) {
        Label = def[nameof(Label)]?.Value<string>()
            ?? throw new ModuleLoadException($"Encountered a rule without a defined {nameof(Label)}.");

        var errpostfx = $" in the rule definition for '{Label}'.";

        var rptch = def[nameof(ReportingChannel)]?.Value<string>();
        if (rptch != null) {
            try {
                ReportingChannel = new EntityName(rptch, EntityType.Channel);
            } catch (FormatException) {
                throw new ModuleLoadException($"'{nameof(ReportingChannel)}' is not defined as a channel{errpostfx}");
            }
        }

        // Regex loading
        var opts = RegexOptions.Compiled | RegexOptions.CultureInvariant;
        // Reminder: in Singleline mode, all contents are subject to the same regex (useful if e.g. spammer separates words line by line)
        opts |= RegexOptions.Singleline;
        // IgnoreCase is enabled by default; must be explicitly set to false
        if (def["IgnoreCase"]?.Value<bool>() ?? true) opts |= RegexOptions.IgnoreCase;
        const string ErrBadRegex = "Unable to parse regular expression pattern";
        var regexRules = new List<Regex>();
        List<string> regexStrings;
        try {
            regexStrings = Utilities.LoadStringOrStringArray(def[nameof(Regex)]);
        } catch (ArgumentNullException) {
            throw new ModuleLoadException($"No patterns were defined under '{nameof(Regex)}'{errpostfx}");
        } catch (ArgumentException) {
            throw new ModuleLoadException($"'{nameof(Regex)}' is not properly defined{errpostfx}");
        }
        foreach (var input in regexStrings) {
            try {
                regexRules.Add(new Regex(input, opts));
            } catch (ArgumentException) {
                throw new ModuleLoadException($"{ErrBadRegex}{errpostfx}");
            }
        }
        Regex = regexRules.AsReadOnly();

        // Filtering
        Filter = new FilterList(def);

        // Misc options
        // IgnoreMods is enabled by default; must be explicitly set to false
        IgnoreMods = def[nameof(IgnoreMods)]?.Value<bool>() ?? true;
        ScanEmbeds = def[nameof(ScanEmbeds)]?.Value<bool>() ?? false; // false by default

        // Load response(s) and response settings
        try {
            Response = Utilities.LoadStringOrStringArray(def[nameof(Response)]).AsReadOnly();
        } catch (ArgumentNullException) {
            throw new ModuleLoadException($"No responses were defined under '{nameof(Response)}'{errpostfx}");
        } catch (ArgumentException) {
            throw new ModuleLoadException($"'{nameof(Response)}' is not properly defined{errpostfx}");
        }
        BanPurgeDays = def[nameof(BanPurgeDays)]?.Value<int>() ?? 0;
        NotifyChannelOfRemoval = def[nameof(NotifyChannelOfRemoval)]?.Value<bool>() ?? true;
        NotifyUserOfRemoval = def[nameof(NotifyUserOfRemoval)]?.Value<bool>() ?? true;
    }

    /// <summary>
    /// Checks the given message to determine if it matches this definition's constraints.
    /// </summary>
    /// <returns>True if match.</returns>
    public bool IsMatch(SocketMessage m, bool senderIsModerator) {
        if (Filter.IsFiltered(m, false)) return false;
        if (senderIsModerator && IgnoreMods) return false;

        foreach (var regex in Regex) {
            if (ScanEmbeds && regex.IsMatch(SerializeEmbed(m.Embeds))) return true;
            if (regex.IsMatch(m.Content)) return true;
        }
        return false;
    }

    private static string SerializeEmbed(IReadOnlyCollection<Embed> e) {
        static string serialize(Embed e) {
            var result = new StringBuilder();
            if (e.Author.HasValue) result.AppendLine($"{e.Author.Value.Name} {e.Author.Value.Url}");
            if (!string.IsNullOrWhiteSpace(e.Title)) result.AppendLine(e.Title);
            if (!string.IsNullOrWhiteSpace(e.Description)) result.AppendLine(e.Description);

            foreach (var f in e.Fields) {
                if (!string.IsNullOrWhiteSpace(f.Name)) result.AppendLine(f.Name);
                if (!string.IsNullOrWhiteSpace(f.Value)) result.AppendLine(f.Value);
            }
            if (e.Footer.HasValue) {
                result.AppendLine(e.Footer.Value.Text ?? "");
            }

            return result.ToString();
        }

        var text = new StringBuilder();
        foreach (var item in e) text.AppendLine(serialize(item));
        return text.ToString();
    }
}
