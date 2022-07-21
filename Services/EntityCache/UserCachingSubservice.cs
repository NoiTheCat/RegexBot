using Microsoft.EntityFrameworkCore;
using RegexBot.Common;
using RegexBot.Data;

namespace RegexBot.Services.EntityCache;
/// <summary>
/// Provides and maintains a database-backed cache of users.
/// It is meant to work as a supplement to Discord.Net's own user caching capabilities. Its purpose is to 
/// provide information on users which the library may not be aware about, such as users no longer in a guild.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
class UserCachingSubservice {
    internal UserCachingSubservice(RegexbotClient bot) {
        bot.DiscordClient.GuildMembersDownloaded += DiscordClient_GuildMembersDownloaded;
        bot.DiscordClient.GuildMemberUpdated += DiscordClient_GuildMemberUpdated;
        bot.DiscordClient.UserUpdated += DiscordClient_UserUpdated;
    }

    private async Task DiscordClient_GuildMembersDownloaded(SocketGuild arg) {
        using var db = new BotDatabaseContext();
        foreach (var user in arg.Users) {
            UpdateUser(user, db);
            UpdateGuildUser(user, db);
        }
        await db.SaveChangesAsync();
    }

    private async Task DiscordClient_GuildMemberUpdated(Discord.Cacheable<SocketGuildUser, ulong> old, SocketGuildUser current) {
        using var db = new BotDatabaseContext();
        UpdateUser(current, db); // Update user data first (avoid potential foreign key constraint violation)
        UpdateGuildUser(current, db);

        await db.SaveChangesAsync();
    }

    private async Task DiscordClient_UserUpdated(SocketUser old, SocketUser current) {
        using var db = new BotDatabaseContext();
        UpdateUser(current, db);
        await db.SaveChangesAsync();
    }

    // IMPORTANT: Do NOT forget to save changes in database after calling this!
    private static void UpdateUser(SocketUser user, BotDatabaseContext db) {
        var uinfo = db.UserCache.Where(c => c.UserId == (long)user.Id).SingleOrDefault();
        if (uinfo == null) {
            uinfo = new() { UserId = (long)user.Id };
            db.UserCache.Add(uinfo);
        }

        uinfo.Username = user.Username;
        uinfo.Discriminator = user.Discriminator;
        uinfo.AvatarUrl = user.GetAvatarUrl(size: 512);
        uinfo.ULastUpdateTime = DateTimeOffset.UtcNow;
    }

    private static void UpdateGuildUser(SocketGuildUser user, BotDatabaseContext db) {
        var guinfo = db.GuildUserCache.Where(c => c.GuildId == (long)user.Guild.Id && c.UserId == (long)user.Id).SingleOrDefault();
        if (guinfo == null) {
            guinfo = new() { GuildId = (long)user.Guild.Id, UserId = (long)user.Id };
            db.GuildUserCache.Add(guinfo);
        }

        guinfo.GULastUpdateTime = DateTimeOffset.UtcNow;
        guinfo.Nickname = user.Nickname;
        // TODO guild-specific avatar, other details?
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

        // Is search actually a ping? Extract ID.
        var m = Utilities.UserMention.Match(search);
        if (m.Success) search = m.Groups["snowflake"].Value;

        // Is search a number? Assume ID, proceed to query.
        if (ulong.TryParse(search, out var searchid)) {    
            var idres = innerQuery(searchid, null);
            if (idres != null) return idres;
        }

        // All of the above failed. Assume the number may be a string to search.
        var namesplit = SplitNameAndDiscriminator(search);
        return innerQuery(null, namesplit);
    }

    // Hooked
    internal CachedGuildUser? DoGuildUserQuery(ulong guildId, string search) {
        static CachedGuildUser? innerQuery(ulong guildId, ulong? sID, (string name, string? disc)? nameSearch) {
            var db = new BotDatabaseContext();
            var query = db.GuildUserCache.Include(gu => gu.User).Where(c => c.GuildId == (long)guildId);
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

        // Is search actually a ping? Extract ID.
        var m = Utilities.UserMention.Match(search);
        if (m.Success) search = m.Groups["snowflake"].Value;

        // Is search a number? Assume ID, proceed to query.
        if (ulong.TryParse(search, out var searchid)) {
            var idres = innerQuery(guildId, searchid, null);
            if (idres != null) return idres;
        }
        
        // All of the above failed. Assume the number may be a string to search.
        var namesplit = SplitNameAndDiscriminator(search);
        return innerQuery(guildId, null, namesplit);
    }

    private static (string, string?) SplitNameAndDiscriminator(string input) {
        string name;
        string? disc = null;
        var split = Utilities.DiscriminatorSearch.Match(input);
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
