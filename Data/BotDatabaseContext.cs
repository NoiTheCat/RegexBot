using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace RegexBot.Data;
/// <summary>
/// Represents a database connection using the settings defined in the bot's global configuration.
/// </summary>
public class BotDatabaseContext : DbContext {
    private static readonly string _connectionString;

    static BotDatabaseContext() {
        // Get our own config loaded just for the SQL stuff
        var conf = new InstanceConfig();
        _connectionString = new NpgsqlConnectionStringBuilder() {
#if DEBUG
            IncludeErrorDetail = true,
#endif
            Host = conf.SqlHost ?? "localhost", // default to localhost
            Database = conf.SqlDatabase,
            Username = conf.SqlUsername,
            Password = conf.SqlPassword
        }.ToString();
    }

    /// <summary>
    /// Retrieves the <seealso cref="CachedUser">user cache</seealso>.
    /// </summary>
    public DbSet<CachedUser> UserCache { get; set; } = null!;

    /// <summary>
    /// Retrieves the <seealso cref="CachedGuildUser">guild user cache</seealso>.
    /// </summary>
    public DbSet<CachedGuildUser> GuildUserCache { get; set; } = null!;

    /// <summary>
    /// Retrieves the <seealso cref="CachedGuildMessage">guild message cache</seealso>.
    /// </summary>
    public DbSet<CachedGuildMessage> GuildMessageCache { get; set; } = null!;

    /// <summary>
    /// Retrieves the <seealso cref="ModLogEntry">moderator logs</seealso>.
    /// </summary>
    public DbSet<ModLogEntry> ModLogs { get; set; } = null!;

    /// <inheritdoc />
    protected sealed override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
         => optionsBuilder
            .UseNpgsql(_connectionString)
            .UseSnakeCaseNamingConvention();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<CachedUser>(entity => entity.Property(e => e.Discriminator).HasMaxLength(4).IsFixedLength());
        modelBuilder.Entity<CachedGuildUser>(e => {
            e.HasKey(p => new { p.GuildId, p.UserId });
            e.Property(p => p.FirstSeenTime).HasDefaultValueSql("now()");
        });
        modelBuilder.Entity<CachedGuildMessage>(e => e.Property(p => p.CreatedAt).HasDefaultValueSql("now()"));
        modelBuilder.HasPostgresEnum<ModLogType>();
        modelBuilder.Entity<ModLogEntry>(e => {
            e.Property(p => p.Timestamp).HasDefaultValueSql("now()");
            e.HasOne(entry => entry.User)
                .WithMany(gu => gu.Logs)
                .HasForeignKey(entry => new { entry.GuildId, entry.UserId });
        });
    }
}
