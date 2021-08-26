using CommandLine;
using CommandLine.Text;
using System;

namespace RegexBot {
    /// <summary>
    /// Command line options
    /// </summary>
    class Options {
        [Option('c', "config", Default = null,
            HelpText = "Custom path to instance configuration. Defaults to config.json in bot directory.")]
        public string ConfigFile { get; set; }

        /// <summary>
        /// Command line arguments parsed here. Depending on inputs, the program can exit here.
        /// </summary>
        public static Options ParseOptions(string[] args) {
            // Parser will not write out to console by itself
            var parser = new Parser(config => config.HelpWriter = null);
            Options opts = null;

            var result = parser.ParseArguments<Options>(args);
            result.WithParsed(p => opts = p);
            result.WithNotParsed(p => {
                // Taking some extra steps to modify the header to make it resemble our welcome message.
                var ht = HelpText.AutoBuild(result);
                ht.Heading += " - https://github.com/NoiTheCat/RegexBot";
                Console.WriteLine(ht.ToString());
                Environment.Exit(1);
            });
            return opts;
        }
    }
}
