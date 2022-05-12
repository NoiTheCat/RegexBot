using Discord.WebSocket;
using RegexBot.Data;
using System.Text.RegularExpressions;

namespace RegexBot.Services.EntityCache;
/// <summary>
/// Provides and maintains a database-backed cache of users.
/// It is meant to work as a supplement to Discord.Net's own user caching capabilities. Its purpose is to 
/// provide information on users which the library may not be aware about, such as users no longer in a guild.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
class UserCachingSubservice {
    private static Regex DiscriminatorSearch { get; } = new(@"(.+)#(\d{4}(?!\d))", RegexOptions.Compiled);

    internal UserCachingSubservice(RegexbotClient bot) {
        bot.DiscordClient.GuildMemberUpdated += DiscordClient_GuildMemberUpdated;
        bot.DiscordClient.UserUpdated += DiscordClient_UserUpdated;
    }

    private async Task DiscordClient_UserUpdated(SocketUser old, SocketUser current) {
        using var db = new BotDatabaseContext();
        UpdateUser(current, db);
        await db.SaveChangesAsync();
    }

    private static void UpdateUser(SocketUser user, BotDatabaseContext db) {
        CachedUser uinfo;
        try {
            uinfo = db.UserCache.Where(c => c.UserId == (long)user.Id).First();
        } catch (InvalidOperationException) {
            uinfo = new() { UserId = (long)user.Id };
            db.UserCache.Add(uinfo);
        }

        uinfo.Username = user.Username;
        uinfo.Discriminator = user.Discriminator;
        uinfo.AvatarUrl = user.GetAvatarUrl(size: 512);
        uinfo.ULastUpdateTime = DateTimeOffset.UtcNow;
    }

    private async Task DiscordClient_GuildMemberUpdated(Discord.Cacheable<SocketGuildUser, ulong> old, SocketGuildUser current) {
        using var db = new BotDatabaseContext();
        UpdateUser(current, db); // Update user data too (avoid potential foreign key constraint violation)

        CachedGuildUser guinfo;
        try {
            guinfo = db.GuildUserCache.Where(c => c.GuildId == (long)current.Guild.Id && c.UserId == (long)current.Id).First();
        } catch (InvalidOperationException) {
            guinfo = new() { GuildId = (long)current.Guild.Id, UserId = (long)current.Id };
            db.GuildUserCache.Add(guinfo);
        }

        guinfo.GULastUpdateTime = DateTimeOffset.UtcNow;
        guinfo.Nickname = current.Nickname;
        // TODO guild-specific avatar, other details?

        await db.SaveChangesAsync();
    }

    // Hooked
    internal CachedUser? DoUserQuery(string search) {
        static CachedUser? innerQuery(ulong? sID, (string name, string? disc)? nameSearch) {
            var db = new BotDatabaseContext();

            var query = db.UserCache.AsQueryable();
            if (sID.HasValue)
                query = query.Where(c => c.UserId == (long)sID.Value);
            if (nameSearch != null) {
                query = query.Where(c => c.Username.ToLower() == nameSearch.Value.name.ToLower());
                if (nameSearch.Value.disc != null) query = query.Where(c => c.Discriminator == nameSearch.Value.disc);
            }
            query = query.OrderByDescending(e => e.ULastUpdateTime);

            return query.SingleOrDefault();
        }

        // Is search just a number? Assume ID, pass it on to the correct place.
        if (ulong.TryParse(search, out var searchid)) {
            var idres = innerQuery(searchid, null);
            if (idres != null) return idres;
        }

        // If the above fails, assume the number may be a string to search.
        var namesplit = SplitNameAndDiscriminator(search);

        return innerQuery(null, namesplit);
    }

    // Hooked
    internal CachedGuildUser? DoGuildUserQuery(ulong guildId, string search) {
        static CachedGuildUser? innerQuery(ulong guildId, ulong? sID, (string name, string? disc)? nameSearch) {
            var db = new BotDatabaseContext();

            var query = db.GuildUserCache.Where(c => c.GuildId == (long)guildId);
            if (sID.HasValue)
                query = query.Where(c => c.UserId == (long)sID.Value);
            if (nameSearch != null) {
                query = query.Where(c => (c.Nickname != null && c.Nickname.ToLower() == nameSearch.Value.name.ToLower()) ||
                    c.User.Username.ToLower() == nameSearch.Value.name.ToLower());
                if (nameSearch.Value.disc != null) query = query.Where(c => c.User.Discriminator == nameSearch.Value.disc);
            }
            query = query.OrderByDescending(e => e.GULastUpdateTime);

            return query.SingleOrDefault();
        }

        // Is search just a number? Assume ID, pass it on to the correct place.
        if (ulong.TryParse(search, out var searchid)) {
            var idres = innerQuery(guildId, searchid, null);
            if (idres != null) return idres;
        }

        // If the above fails, assume the number may be a string to search.
        var namesplit = SplitNameAndDiscriminator(search);

        return innerQuery(guildId, null, namesplit);
    }

    private static (string, string?) SplitNameAndDiscriminator(string input) {
        string name;
        string? disc = null;
        var split = DiscriminatorSearch.Match(input);
        if (split.Success) {
            name = split.Groups[1].Value;
            disc = split.Groups[2].Value;
        } else {
            name = input;
        }

        // Also strip leading '@' from search
        if (name.Length > 0 && name[0] == '@') name = name[1..];

        return (name, disc);
    }
}
