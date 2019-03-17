using Discord.WebSocket;
using NpgsqlTypes;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kerobot.Services.EntityCache
{
    /// <summary>
    /// Provides and maintains a database-backed cache of users.
    /// It is meant to work as an addition to Discord.Net's own user caching capabilities. Its purpose is to 
    /// provide information on users which the library may not be aware about, such as users no longer in a guild.
    /// </summary>
    class UserCache
    {
        private Kerobot _kb;

        internal UserCache(Kerobot kb)
        {
            _kb = kb;
            CreateDatabaseTablesAsync().Wait();

            kb.DiscordClient.GuildMemberUpdated += DiscordClient_GuildMemberUpdated;
            kb.DiscordClient.UserUpdated += DiscordClient_UserUpdated;
        }

        #region Database setup
        public const string GlobalUserTable = "cache_userdata";
        public const string GuildUserTable = "cache_guildmemberdata";
        public const string UserView = "cache_guildusers"; // <- intended way to access data

        private async Task CreateDatabaseTablesAsync()
        {
            using (var db = await _kb.GetOpenNpgsqlConnectionAsync())
            {
                using (var c = db.CreateCommand())
                {
                    c.CommandText = $"create table if not exists {GlobalUserTable} (" +
                        "user_id bigint primary key, " +
                        "cache_update_time timestamptz not null, " + // TODO auto update w/ trigger?
                        "username text not null, " +
                        "discriminator text not null, " +
                        "avatar_url text null" +
                        ")";
                    await c.ExecuteNonQueryAsync();
                }

                using (var c = db.CreateCommand())
                {
                    c.CommandText = $"create table if not exists {GuildUserTable} (" +
                        $"user_id bigint references {GlobalUserTable}, " +
                        "guild_id bigint, " + // TODO foreign key reference?
                        "first_seen timestamptz not null default NOW(), " + // TODO also make immutable w/ trigger?
                        "cache_update_time timestamptz not null, " + // TODO auto update w/ trigger?
                        "nickname text null, " +
                        "primary key (user_id, guild_id)" +
                        ")";
                    await c.ExecuteNonQueryAsync();
                }
                // note to self: https://stackoverflow.com/questions/9556474/how-do-i-automatically-update-a-timestamp-in-postgresql
                
                using (var c = db.CreateCommand())
                {
                    // NOTE: CachedUser constructor is highly dependent of the row order specified here.
                    // Any changes here must be reflected there.
                    c.CommandText = $"create or replace view {UserView} as " +
                        $"select {GlobalUserTable}.user_id, {GuildUserTable}.guild_id, {GuildUserTable}.first_seen, " +
                        $"{GuildUserTable}.cache_update_time, " +
                        $"{GlobalUserTable}.username, {GlobalUserTable}.discriminator, {GlobalUserTable}.nickname, " +
                        $"{GlobalUserTable}.avatar_url " +
                        $"from {GlobalUserTable} join {GuildUserTable} on {GlobalUserTable}.user_id = {GuildUserTable}.user_id";
                    await c.ExecuteNonQueryAsync();
                    // TODO consolidate both tables' cache_update_time, show the greater value.
                    // Right now we will have just the guild table's data visible.
                }
            }
        }
        #endregion

        #region Database updates
        private async Task DiscordClient_UserUpdated(SocketUser old, SocketUser current)
        {
            using (var db = await _kb.GetOpenNpgsqlConnectionAsync())
            {
                using (var c = db.CreateCommand())
                {
                    c.CommandText = $"insert into {GlobalUserTable} " +
                        "(user_id, cache_update_time, username, discriminator, avatar_url) values " +
                        "(@Uid, now(), @Uname, @Disc, @Aurl) " +
                        "on conflict (user_id) do update " +
                        "set cache_update_time = EXCLUDED.cache_update_time, username = EXCLUDED.username, " +
                        "discriminator = EXCLUDED.discriminator, avatar_url = EXCLUDED.avatar_url";
                    c.Parameters.Add("@Uid", NpgsqlDbType.Bigint).Value = current.Id;
                    c.Parameters.Add("@Uname", NpgsqlDbType.Text).Value = current.Username;
                    c.Parameters.Add("@Disc", NpgsqlDbType.Text).Value = current.Discriminator;
                    var aurl = c.Parameters.Add("@Aurl", NpgsqlDbType.Text);
                    var aurlval = current.GetAvatarUrl(Discord.ImageFormat.Png, 1024);
                    if (aurlval != null) aurl.Value = aurlval;
                    else aurl.Value = DBNull.Value;

                    c.Prepare();
                    await c.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task DiscordClient_GuildMemberUpdated(SocketGuildUser old, SocketGuildUser current)
        {
            using (var db = await _kb.GetOpenNpgsqlConnectionAsync())
            {
                using (var c = db.CreateCommand())
                {
                    c.CommandText = $"insert into {GuildUserTable} " +
                        "(user_id, guild_id, cache_update_time, nickname) values " +
                        "(@Uid, @Gid, now(), @Nname) " +
                        "on conflict (user_id) do update " +
                        "set cache_update_time = EXCLUDED.cache_update_time, username = EXCLUDED.username, " +
                        "discriminator = EXCLUDED.discriminator, avatar_url = EXCLUDED.avatar_url";
                    c.Parameters.Add("@Uid", NpgsqlDbType.Bigint).Value = current.Id;
                    c.Parameters.Add("@Gid", NpgsqlDbType.Bigint).Value = current.Guild.Id;
                    var nname = c.Parameters.Add("@Nname", NpgsqlDbType.Text);
                    if (current.Nickname != null) nname.Value = current.Nickname;
                    else nname.Value = DBNull.Value;

                    c.Prepare();
                    await c.ExecuteNonQueryAsync();
                }
            }
        }
        #endregion

        #region Querying
        private static Regex DiscriminatorSearch = new Regex(@"(.+)#(\d{4}(?!\d))", RegexOptions.Compiled);

        /// <summary>
        /// See <see cref="Kerobot.EcQueryUser(ulong, string)"/>.
        /// </summary>
        internal async Task<CachedUser> Query(ulong guildID, string search)
        {
            // Is search just a number? Assume ID, pass it on to the correct place.
            // If it fails, assume the number may be a username.
            if (ulong.TryParse(search, out var searchid))
            {
                var idres = await InternalDoQuery(guildID, searchid, null, null);
                if (idres != null) return idres;
            }

            // Split name/discriminator
            string name, disc;
            var split = DiscriminatorSearch.Match(search);
            if (split.Success)
            {
                name = split.Groups[1].Value;
                disc = split.Groups[2].Value;
            }
            else
            {
                name = search;
                disc = null;
            }

            // Strip leading @ from username, if any
            if (name.Length > 0 && name[0] == '@') name = name.Substring(1);

            // Ready to query
            return await InternalDoQuery(guildID, null, name, disc);
            // TODO exception handling
        }

        private async Task<CachedUser> InternalDoQuery(ulong guildId, ulong? sID, string sName, string sDisc)
        {
            using (var db = await _kb.GetOpenNpgsqlConnectionAsync())
            {
                var c = db.CreateCommand();
                c.CommandText = $"select * from {UserView} " +
                    "where guild_id = @Gid";
                c.Parameters.Add("@Gid", NpgsqlDbType.Bigint).Value = guildId;

                if (sID.HasValue)
                {
                    c.CommandText += " and user_id = @Uid";
                    c.Parameters.Add("@Uid", NpgsqlDbType.Bigint).Value = sID.Value;
                }

                if (sName != null)
                {
                    c.CommandText += " and username = @Uname";
                    c.Parameters.Add("@Uname", NpgsqlDbType.Text).Value = sName;
                    if (sDisc != null) // only search discriminator if name has been provided
                    {
                        c.CommandText += " and discriminator = @Udisc";
                        c.Parameters.Add("@Udisc", NpgsqlDbType.Text).Value = sDisc;
                    }
                }

                c.CommandText += " order by cache_update_time desc limit 1";
                c.Prepare();
                using (var r = await c.ExecuteReaderAsync())
                {
                    if (await r.ReadAsync()) return new CachedUser(r);
                    return null;
                }
            }
        }
        #endregion
    }
}
