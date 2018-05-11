using Discord;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Threading.Tasks;

namespace Kerobot.Services.Logging
{
    /// <summary>
    /// Implements logging for the whole program.
    /// </summary>
    class LoggingService : Service
    {
        // Note: Service.Log's functionality is implemented here. Don't use it within this class.
        // If necessary, use DoInstanceLogAsync instead.

        internal LoggingService(Kerobot kb) : base(kb)
        {
            // Create global instance log table
            async Task CreateGlobalTable()
            {
                using (var db = await Kerobot.GetOpenNpgsqlConnectionAsync(null))
                    await CreateDatabaseTablesAsync(db);
            }
            CreateGlobalTable().Wait();

            // Discord.Net log handling (client logging option is specified in Program.cs)
            kb.DiscordClient.Log += DiscordClient_Log;
        }

        /// <summary>
        /// Discord.Net logging events handled here.
        /// Only events with high severity are placed in the log. Others are just printed to console.
        /// </summary>
        private async Task DiscordClient_Log(LogMessage arg)
        {
            var ts = DateTimeOffset.UtcNow;
            bool important = arg.Severity > LogSeverity.Info;
            string msg = $"[{Enum.GetName(typeof(LogSeverity), arg.Severity)}] {arg.Message}";
            const string logSource = "Discord.Net";

            if (important)
            {
                // Note: Using external method here!
                await Kerobot.InstanceLogAsync(true, logSource, msg);
            }
            else
            {
                FormatToConsole(DateTimeOffset.UtcNow, logSource, msg);
            }
        }

        const string TableLog = "logging";
        public override async Task CreateDatabaseTablesAsync(NpgsqlConnection db)
        {
            using (var c = db.CreateCommand())
            {
                c.CommandText = $"create table if not exists {TableLog} ("
                    + "log_id serial primary key, "
                    + "log_timestamp timestamptz not null, "
                    + "log_source text not null, "
                    + "message text not null"
                    + ")";
                await c.ExecuteNonQueryAsync();
            }
        }
        private async Task TableInsertAsync(NpgsqlConnection db, DateTimeOffset timestamp, string source, string message)
        {
            using (var c = db.CreateCommand())
            {
                c.CommandText = $"insert into {TableLog} (log_timestamp, log_source, message) values"
                    + "(@Ts, @Src, @Msg)";
                c.Parameters.Add("@Ts", NpgsqlDbType.TimestampTZ).Value = timestamp;
                c.Parameters.Add("@Src", NpgsqlDbType.Text).Value = source;
                c.Parameters.Add("@Msg", NpgsqlDbType.Text).Value = message;
                c.Prepare();
                await c.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// All console writes originate here.
        /// Takes incoming details of a log message. Formats the incoming information in a
        /// consistent format before writing out the result to console.
        /// </summary>
        private void FormatToConsole(DateTimeOffset timestamp, string source, string message)
        {
            var prefix = $"[{timestamp:u}] [{source}] ";
            foreach (var line in message.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                Console.WriteLine(prefix + line);
            }
        }

        /// <summary>
        /// See <see cref="Kerobot.InstanceLogAsync(bool, string, string)"/>
        /// </summary>
        public async Task DoInstanceLogAsync(bool report, string source, string message)
        {
            FormatToConsole(DateTimeOffset.UtcNow, source, message);

            Exception insertException = null;
            try
            {
                using (var db = await Kerobot.GetOpenNpgsqlConnectionAsync(null))
                {
                    await TableInsertAsync(db, DateTimeOffset.UtcNow, source, message);
                }
            }
            catch (Exception ex)
            {
                // This is not good. Resorting to plain console write to report the issue.
                // Let's hope a warning reaches the reporting channel.
                Console.WriteLine("!!! Error during recording to instance log: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                insertException = ex;
            }

            // Report to logging channel if necessary and possible
            var (g, c) = Kerobot.Config.InstanceLogReportTarget;
            if ((insertException != null || report) &&
                g != 0 && c != 0 && Kerobot.DiscordClient.ConnectionState == ConnectionState.Connected)
            {
                var ch = Kerobot.DiscordClient.GetGuild(g)?.GetTextChannel(c);
                if (ch == null) return; // not connected, or channel doesn't exist.

                if (insertException != null)
                {
                    // Attempt to report instance logging failure to the reporting channel
                    try
                    {
                        EmbedBuilder e = new EmbedBuilder()
                        {
                            Footer = new EmbedFooterBuilder() { Text = Name },
                            Timestamp = DateTimeOffset.UtcNow,
                            Description = "Error during recording to instance log.\nCheck the console.",
                            Color = Color.DarkRed
                        };
                        await ch.SendMessageAsync("", embed: e.Build());
                    }
                    catch
                    {
                        return; // Give up
                    }
                }

                if (report)
                {
                    try
                    {
                        EmbedBuilder e = new EmbedBuilder()
                        {
                            Footer = new EmbedFooterBuilder() { Text = source },
                            Timestamp = DateTimeOffset.UtcNow,
                            Description = message
                        };
                        await ch.SendMessageAsync("", embed: e.Build());
                    }
                    catch (Discord.Net.HttpException ex)
                    {
                        await DoInstanceLogAsync(false, Name, "Failed to send message to reporting channel: " + ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// See <see cref="Kerobot.GuildLogAsync(ulong, string, string)"/>
        /// </summary>
        public async Task DoGuildLogAsync(ulong guild, string source, string message)
        {
            try
            {
                using (var db = await Kerobot.GetOpenNpgsqlConnectionAsync(guild))
                {
                    await TableInsertAsync(db, DateTimeOffset.UtcNow, source, message);
                }
            }
            catch (Exception ex)
            {
                // Probably a bad idea, but...
                await DoInstanceLogAsync(true, this.Name, "Failed to store guild log item: " + ex.Message);
                // Stack trace goes to console only.
                FormatToConsole(DateTime.UtcNow, this.Name, ex.StackTrace);
            }
        }
    }
}
