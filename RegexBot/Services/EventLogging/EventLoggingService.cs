using System;
using System.Threading.Tasks;
using Discord;
using NpgsqlTypes;

namespace RegexBot.Services.EventLogging
{
    /// <summary>
    /// Implements logging. Logging is distinguished into two types: Instance and per-guild.
    /// Instance logs are messages of varying importance to the bot operator. Guild logs are messages that can be seen
    /// by moderators of a particular guild. All log messages are backed by database.
    /// Instance logs are stored as guild ID 0.
    /// </summary>
    class EventLoggingService : Service
    {
        // Note: Service.Log's functionality is implemented here. Don't use it within this class.
        // If necessary, use DoInstanceLogAsync instead.

        internal EventLoggingService(RegexbotClient bot) : base(bot)
        {
            // Create logging table
            CreateDatabaseTablesAsync().Wait();

            // Discord.Net log handling (client logging option is specified in Program.cs)
            bot.DiscordClient.Log += DiscordClient_Log;

            // Ready message too
            bot.DiscordClient.Ready += 
                async delegate { await DoInstanceLogAsync(true, nameof(RegexBot), "Connected and ready."); };
        }

        /// <summary>
        /// Discord.Net logging events handled here.
        /// Only events with high importance are kept. Others are just printed to console.
        /// </summary>
        private async Task DiscordClient_Log(LogMessage arg)
        {
            bool important = arg.Severity != LogSeverity.Info;
            string msg = $"[{Enum.GetName(typeof(LogSeverity), arg.Severity)}] {arg.Message}";
            const string logSource = "Discord.Net";
            if (arg.Exception != null) msg += "\n```\n" + arg.Exception.ToString() + "\n```";

            if (important) await DoInstanceLogAsync(true, logSource, msg);
            else FormatToConsole(DateTimeOffset.UtcNow, logSource, msg);
        }

        #region Database
        const string TableLog = "program_log";
        private async Task CreateDatabaseTablesAsync()
        {
            using (var db = await BotClient.GetOpenNpgsqlConnectionAsync())
            {
                using (var c = db.CreateCommand())
                {
                    c.CommandText = $"create table if not exists {TableLog} ("
                        + "log_id serial primary key, "
                        + "guild_id bigint not null, "
                        + "log_timestamp timestamptz not null, "
                        + "log_source text not null, "
                        + "message text not null"
                        + ")";
                    await c.ExecuteNonQueryAsync();
                }
                using (var c = db.CreateCommand())
                {
                    c.CommandText = "create index if not exists " +
                        $"{TableLog}_guild_id_idx on {TableLog} (guild_id)";
                    await c.ExecuteNonQueryAsync();
                }
            }

        }
        private async Task TableInsertAsync(ulong guildId, DateTimeOffset timestamp, string source, string message)
        {
            using (var db = await BotClient.GetOpenNpgsqlConnectionAsync())
            {
                using (var c = db.CreateCommand())
                {
                    c.CommandText = $"insert into {TableLog} (guild_id, log_timestamp, log_source, message) values"
                        + "(@Gid, @Ts, @Src, @Msg)";
                    c.Parameters.Add("@Gid", NpgsqlDbType.Bigint).Value = (long)guildId;
                    c.Parameters.Add("@Ts", NpgsqlDbType.TimestampTz).Value = timestamp;
                    c.Parameters.Add("@Src", NpgsqlDbType.Text).Value = source;
                    c.Parameters.Add("@Msg", NpgsqlDbType.Text).Value = message;
                    c.Prepare();
                    await c.ExecuteNonQueryAsync();
                }
            }
        }
        #endregion

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
        /// See <see cref="RegexbotClient.InstanceLogAsync(bool, string, string)"/>
        /// </summary>
        public async Task DoInstanceLogAsync(bool report, string source, string message)
        {
            FormatToConsole(DateTimeOffset.UtcNow, source, message);

            Exception insertException = null;
            try
            {
                await TableInsertAsync(0, DateTimeOffset.UtcNow, source, message);
            }
            catch (Exception ex)
            {
                // Not good. Resorting to plain console write to report the error.
                Console.WriteLine("!!! Error during recording to instance log: " + ex.Message);
                Console.WriteLine(ex.StackTrace);

                // Attempt to pass this error to the reporting channel.
                insertException = ex;
            }

            // Report to logging channel if necessary and possible
            // TODO replace with webhook?
            var (g, c) = BotClient.Config.InstanceLogReportTarget;
            if ((insertException != null || report) &&
                g != 0 && c != 0 && BotClient.DiscordClient.ConnectionState == ConnectionState.Connected)
            {
                var ch = BotClient.DiscordClient.GetGuild(g)?.GetTextChannel(c);
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
                            Description = "Error during recording to instance log: `" +
                                insertException.Message + "`\nCheck the console.",
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
        /// See <see cref="RegexbotClient.GuildLogAsync(ulong, string, string)"/>
        /// </summary>
        public async Task DoGuildLogAsync(ulong guild, string source, string message)
        {
            try
            {
                await TableInsertAsync(guild, DateTimeOffset.UtcNow, source, message);
#if DEBUG
                FormatToConsole(DateTimeOffset.UtcNow, $"DEBUG {guild} - {source}", message);
#endif
            }
            catch (Exception ex)
            {
                // This is probably a terrible idea, but...
                await DoInstanceLogAsync(true, this.Name, "Failed to store guild log item: " + ex.Message);
                // Stack trace goes to console only.
                FormatToConsole(DateTime.UtcNow, this.Name, ex.StackTrace);
            }
        }
    }
}
