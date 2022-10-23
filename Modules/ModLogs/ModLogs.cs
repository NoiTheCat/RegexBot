using System.Text;

namespace RegexBot.Modules.ModLogs;
/// <summary>
/// Logs certain events of note to a database for moderators to keep track of user behavior.
/// Makes use of a helper class, <see cref="MessageCache"/>.
/// </summary>
[RegexbotModule]
internal partial class ModLogs : RegexbotModule {
    // TODO consider resurrecting 2.x idea of logging actions to db, making it searchable?

    public ModLogs(RegexbotClient bot) : base(bot) {
        // TODO missing logging features: joins, leaves, user edits (nick/username/discr)
        DiscordClient.MessageDeleted += HandleDelete;
        bot.SharedEventReceived += HandleReceivedSharedEvent;
    }

    public override Task<object?> CreateGuildStateAsync(ulong guildID, JToken? config) {
        if (config == null) return Task.FromResult<object?>(null);
        if (config.Type != JTokenType.Object)
            throw new ModuleLoadException("Configuration for this section is invalid.");
        var newconf = new ModuleConfig((JObject)config);
        Log(DiscordClient.GetGuild(guildID), $"Writing logs to {newconf.ReportingChannel}.");
        return Task.FromResult<object?>(new ModuleConfig((JObject)config));
    }

    private async Task HandleReceivedSharedEvent(ISharedEvent ev) {
        if (ev is MessageCacheUpdateEvent upd) await HandleUpdate(upd.OldMessage, upd.NewMessage);    
        else if (ev is Data.ModLogEntry log) await HandleLog(log);
    }

    private static string MakeTimestamp(DateTimeOffset time) {
        var result = new StringBuilder();
        //result.Append(time.ToString("yyyy-MM-dd hh:mm:ss"));
        result.Append($"<t:{time.ToUnixTimeSeconds()}:f>");

        var now = DateTimeOffset.UtcNow;
        var diff = now - time;
        if (diff < new TimeSpan(3, 0, 0, 0)) {
            // Difference less than 3 days. Generate relative time format.
            result.Append(" - ");

            if (diff.TotalSeconds < 60) {
                // Under a minute ago. Show only seconds.
                result.Append((int)Math.Ceiling(diff.TotalSeconds) + "s ago");
            } else {
                // over a minute. Show days, hours, minutes, seconds.
                var ts = (int)Math.Ceiling(diff.TotalSeconds);
                var m = ts % 3600 / 60;
                var h = ts % 86400 / 3600;
                var d = ts / 86400;

                if (d > 0) result.AppendFormat("{0}d{1}h{2}m", d, h, m);
                else if (h > 0) result.AppendFormat("{0}h{1}m", h, m);
                else result.AppendFormat("{0}m", m);
                result.Append(" ago");
            }
        }

        return result.ToString();
    }

    private static string GetDefaultAvatarUrl(string discriminator) {
        var discVal = int.Parse(discriminator);
        return $"https://cdn.discordapp.com/embed/avatars/{discVal % 5}.png";
    }
}