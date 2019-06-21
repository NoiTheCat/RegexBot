using Discord;
using Discord.WebSocket;
using Kerobot.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Kerobot.Modules.RegexModerator
{
    /// <summary>
    /// Representation of a single RegexModerator rule for a guild.
    /// Data in this class is immutable. Contains various helper methods.
    /// </summary>
    [DebuggerDisplay("RM rule '{_label}' for {_guild}")]
    class ConfDefinition
    {
        public string Label { get; }

        // Matching settings
        readonly IEnumerable<Regex> _regex;
        readonly FilterList _filter;
        readonly bool _ignoreMods;
        readonly bool _embedScan;

        // Response settings
        public string ReplyInChannel { get; }
        public string ReplyInDM { get; }
        public EntityName RoleAdd { get; } // keep in mind it's possible to have both add and remove role available at once
        public EntityName RoleRemove { get; }
        //readonly bool _rRolePersist; // TODO use when feature exists
        public EntityName ReportingChannel { get; }
        public Kerobot.RemovalType RemovalAction { get; } // ban, kick?
        public int BanPurgeDays { get; }
        public string RemovalReason { get; } // reason to place into audit log, notification
        public bool RemovalSendUserNotification; // send ban/kick notification to user?
        public bool DeleteMessage { get; }

        public ConfDefinition(JObject def)
        {
            Label = def["Label"].Value<string>();
            if (string.IsNullOrWhiteSpace(Label))
                throw new ModuleLoadException("A rule does not have a label defined.");

            string errpostfx = $" in the rule definition for '{Label}'.";

            // Regex loading
            var opts = RegexOptions.Compiled | RegexOptions.CultureInvariant;
            // TODO consider adding an option to specify Singleline and Multiline mode. Defaulting to Singleline.
            opts |= RegexOptions.Singleline;
            // IgnoreCase is enabled by default; must be explicitly set to false
            bool? rxci = def["IgnoreCase"]?.Value<bool>();
            if (rxci ?? true) opts |= RegexOptions.IgnoreCase;

            const string ErrNoRegex = "Regular expression patterns are not defined";
            var rxs = new List<Regex>();
            var rxconf = def["regex"];
            if (rxconf == null) throw new ModuleLoadException(ErrNoRegex + errpostfx);
            if (rxconf.Type == JTokenType.Array)
            {
                foreach (var input in rxconf.Values<string>())
                {
                    try
                    {
                        // TODO HIGH IMPORTANCE: sanitize input regex; don't allow inline editing of options
                        Regex r = new Regex(input, opts);
                        rxs.Add(r);
                    }
                    catch (ArgumentException)
                    {
                        throw new ModuleLoadException("Unable to parse regular expression pattern" + errpostfx);
                    }
                }
            }
            else
            {
                string rxstr = rxconf.Value<string>();
                try
                {
                    Regex r = new Regex(rxstr, opts);
                    rxs.Add(r);
                }
                catch (Exception ex) when (ex is ArgumentException || ex is NullReferenceException)
                {
                    throw new ModuleLoadException("Unable to parse regular expression pattern" + errpostfx);
                }
            }
            if (rxs.Count == 0)
            {
                throw new ModuleLoadException(ErrNoRegex + errpostfx);
            }
            _regex = rxs.ToArray();

            // Filtering
            _filter = new FilterList(def);

            // Misc options
            // IgnoreMods is enabled by default; must be explicitly set to false
            bool? bypass = def["IgnoreMods"]?.Value<bool>();
            _ignoreMods = bypass ?? true;

            bool? embedScan = def["EmbedScanMode"]?.Value<bool>();
            _embedScan = embedScan ?? false; // false by default

            // Response options
            var resp = def["Response"] as JObject;
            if (resp == null)
                throw new ModuleLoadException("Cannot find a valid response section" + errpostfx);

            ReplyInChannel = resp[nameof(ReplyInChannel)]?.Value<string>();
            ReplyInDM = resp[nameof(ReplyInDM)]?.Value<string>();

            const string ErrRole = "The role value specified is not properly defined as a role";
            // TODO make this error message nicer
            var rolestr = resp[nameof(RoleAdd)]?.Value<string>();
            if (!string.IsNullOrWhiteSpace(rolestr))
            {
                RoleAdd = new EntityName(rolestr);
                if (RoleAdd.Type != EntityType.Role) throw new ModuleLoadException(ErrRole + errpostfx);
            }
            else RoleAdd = null;
            rolestr = resp[nameof(RoleRemove)]?.Value<string>();
            if (!string.IsNullOrWhiteSpace(rolestr))
            {
                RoleRemove = new EntityName(rolestr);
                if (RoleRemove.Type != EntityType.Role) throw new ModuleLoadException(ErrRole + errpostfx);
            }
            else RoleRemove = null;

            //_rRolePersist = resp["RolePersist"]?.Value<bool>() ?? false;

            var reportstr = resp[nameof(ReportingChannel)]?.Value<string>();
            if (!string.IsNullOrWhiteSpace(reportstr))
            {
                ReportingChannel = new EntityName(reportstr);
                if (ReportingChannel.Type != EntityType.Channel)
                    throw new ModuleLoadException("The reporting channel specified is not properly defined as a channel" + errpostfx);
            }
            else ReportingChannel = null;

            var removestr = resp[nameof(RemovalAction)]?.Value<string>();
            // accept values ban, kick, none
            switch (removestr)
            {
                case "ban": RemovalAction = Kerobot.RemovalType.Ban; break;
                case "kick": RemovalAction = Kerobot.RemovalType.Kick; break;
                case "none": RemovalAction = Kerobot.RemovalType.None; break;
                default:
                    if (removestr != null)
                        throw new ModuleLoadException("RemoveAction is not set to a proper value" + errpostfx);
                    break;
            }

            // TODO extract BanPurgeDays
            int? banpurgeint;
            try { banpurgeint = resp[nameof(BanPurgeDays)]?.Value<int>(); }
            catch (InvalidCastException) { throw new ModuleLoadException("BanPurgeDays must be a numeric value."); }
            if (banpurgeint.HasValue)
            {
                if (banpurgeint > 7 || banpurgeint < 0)
                    throw new ModuleLoadException("BanPurgeDays must be a value between 0 and 7 inclusive.");
                BanPurgeDays = banpurgeint ?? 0;
            }

            RemovalReason = resp[nameof(RemovalReason)]?.Value<string>();

            RemovalSendUserNotification = resp[nameof(RemovalSendUserNotification)]?.Value<bool>() ?? false;

            DeleteMessage = resp[nameof(DeleteMessage)]?.Value<bool>() ?? false;
        }

        /// <summary>
        /// Checks the given message to determine if it matches this definition's constraints.
        /// </summary>
        /// <returns>True if match.</returns>
        public bool IsMatch(SocketMessage m, bool senderIsModerator)
        {
            // TODO keep id: true or false?
            if (_filter.IsFiltered(m, false)) return false;
            if (senderIsModerator && _ignoreMods) return false;

            var matchText = _embedScan ? SerializeEmbed(m.Embeds) : m.Content;

            foreach (var regex in _regex)
            {
                // TODO enforce maximum execution time
                // TODO multi-processing of multiple regexes?
                // TODO metrics: temporary tracking of regex execution times
                if (regex.IsMatch(m.Content)) return true;
            }

            return false;
        }

        private string SerializeEmbed(IReadOnlyCollection<Embed> e)
        {
            var text = new StringBuilder();
            foreach (var item in e) text.AppendLine(SerializeEmbed(item));
            return text.ToString();
        }

        /// <summary>
        /// Converts an embed to a plain string for easier matching.
        /// </summary>
        private string SerializeEmbed(Embed e)
        {
            StringBuilder result = new StringBuilder();
            if (e.Author.HasValue) result.AppendLine(e.Author.Value.Name ?? "" + e.Author.Value.Url ?? "");

            if (!string.IsNullOrWhiteSpace(e.Title)) result.AppendLine(e.Title);
            if (!string.IsNullOrWhiteSpace(e.Description)) result.AppendLine(e.Description);

            foreach (var f in e.Fields)
            {
                if (!string.IsNullOrWhiteSpace(f.Name)) result.AppendLine(f.Name);
                if (!string.IsNullOrWhiteSpace(f.Value)) result.AppendLine(f.Value);
            }

            if (e.Footer.HasValue)
            {
                result.AppendLine(e.Footer.Value.Text ?? "");
            }

            return result.ToString();
        }
    }
}
