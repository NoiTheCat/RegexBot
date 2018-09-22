using Discord.WebSocket;
using Kerobot.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Kerobot.Modules.AutoResponder
{
    /// <summary>
    /// Representation of a single <see cref="AutoResponder"/> configuration definition.
    /// </summary>
    class Definition
    {
        private static readonly Random Chance = new Random();
        
        public string Label { get; }
        public IEnumerable<Regex> Regex { get; }
        public IReadOnlyList<string> Response { get; }
        public FilterList Filter { get; }
        public RateLimit<ulong> RateLimit { get; }
        public double RandomChance { get; }

        /// <summary>
        /// Creates an instance based on JSON configuration.
        /// </summary>
        /// <param name="config"></param>
        public Definition(JProperty incoming)
        {
            Label = incoming.Name;
            if (incoming.Value.Type != JTokenType.Object)
                throw new ModuleLoadException($"Value of {nameof(AutoResponder)} definition must be a JSON object.");
            var data = (JObject)incoming.Value;

            // error message postfix
            var errorpfx = $" in AutoRespond definition '{Label}'.";

            // Parse regex
            const RegexOptions rxopts = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline;
            var regexes = new List<Regex>();
            var rxconf = data["regex"];
            if (rxconf == null) throw new ModuleLoadException("No regular expression patterns defined" + errorpfx);
            // Accepting either array or single string
            // TODO detection of regex values that go around these options or attempt advanced functionality
            // TODO repeated code! simplify this a little.
            if (rxconf.Type == JTokenType.Array)
            {
                foreach (var input in rxconf.Values<string>())
                {
                    try
                    {
                        var r = new Regex(input, rxopts);
                        regexes.Add(r);
                    }
                    catch (ArgumentException)
                    {
                        throw new ModuleLoadException($"Failed to parse regular expression pattern '{input}'{errorpfx}");
                    }
                }
            }
            else if (rxconf.Type == JTokenType.String)
            {
                var rxstr = rxconf.Value<string>();
                try
                {
                    var r = new Regex(rxstr, rxopts);
                    regexes.Add(r);
                }
                catch (ArgumentException)
                {
                    throw new ModuleLoadException($"Failed to parse regular expression pattern '{rxstr}'{errorpfx}");
                }
            }
            else
            {
                // TODO fix up wording of error message here
                throw new ModuleLoadException("Unacceptable value given as a regular expession" + errorpfx);
            }
            Regex = regexes.AsReadOnly();

            // Get response
            // TODO repeated code here also! maybe create a helper method for reading either a single string or string array.
            var replyconf = data["reply"];
            if (replyconf.Type == JTokenType.String)
            {
                var str = replyconf.Value<string>();
                Response = new List<string>() { str }.AsReadOnly();
            }
            else if (replyconf.Type == JTokenType.Array)
            {
                Response = new List<string>(replyconf.Values<string>()).AsReadOnly();
            }

            // Filtering
            Filter = new FilterList(data);

            // Rate limiting
            string rlstr = data["ratelimit"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(rlstr))
            {
                RateLimit = new RateLimit<ulong>();
            }
            else
            {
                if  (ushort.TryParse(rlstr, out var rlval))
                {
                    RateLimit = new RateLimit<ulong>(rlval);
                }
                else
                {
                    throw new ModuleLoadException("Invalid rate limit value" + errorpfx);
                }
            }

            // Random chance parameter
            string randstr = data["RandomChance"]?.Value<string>();
            double randval;
            if (string.IsNullOrWhiteSpace(randstr))
            {
                randval = double.NaN;
            }
            else
            {
                if (!double.TryParse(randstr, out randval))
                {
                    throw new ModuleLoadException("Random value is invalid (unable to parse)" + errorpfx);
                }
                if (randval > 1 || randval < 0)
                {
                    throw new ModuleLoadException("Random value is invalid (not between 0 and 1)" + errorpfx);
                }
            }
            RandomChance = randval;
        }

        /// <summary>
        /// Checks the given message to determine if it matches this rule's constraints.
        /// </summary>
        /// <returns>True if the rule's response(s) should be executed.</returns>
        public bool Match(SocketMessage m)
        {
            // Filter check
            if (Filter.IsFiltered(m, true)) return false;

            // Match check
            bool matchFound = false;
            foreach (var item in Regex)
            {
                // TODO determine maximum execution time for a regular expression match
                if (item.IsMatch(m.Content))
                {
                    matchFound = true;
                    break;
                }
            }
            if (!matchFound) return false;

            // Rate limit check - currently per channel
            if (!RateLimit.Permit(m.Channel.Id)) return false;

            // Random chance check
            if (!double.IsNaN(RandomChance))
            {
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
        public string GetResponse()
        {
            // TODO feature request: option to show responses in order instead of random
            if (Response.Count == 1) return Response[0];
            return Response[Chance.Next(0, Response.Count - 1)];
        }
    }
}
