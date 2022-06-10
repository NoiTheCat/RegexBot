using Microsoft.EntityFrameworkCore;

namespace RegexBot.Data;

public class BotDatabaseContext : DbContext {
    private static string? _npgsqlConnectionString;
    internal static string PostgresConnectionString {
#if DEBUG
        get {
            if (_npgsqlConnectionString != null) return _npgsqlConnectionString;
            Console.WriteLine($"{nameof(RegexBot)} - {nameof(BotDatabaseContext)} note: Using hardcoded connection string!");
            return _npgsqlConnectionString ?? "Host=localhost;Username=regexbot;Password=rb";
        }
#else
        get => _npgsqlConnectionString!;
#endif
        set => _npgsqlConnectionString ??= value;
    }

    public DbSet<GuildLogLine> GuildLog { get; set; } = null!;
    public DbSet<CachedUser> UserCache { get; set; } = null!;
    public DbSet<CachedGuildUser> GuildUserCache { get; set; } = null!;
    public DbSet<CachedGuildMessage> GuildMessageCache { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
         => optionsBuilder
            .UseNpgsql(PostgresConnectionString)
            .UseSnakeCaseNamingConvention();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<GuildLogLine>(entity => entity.Property(e => e.Timestamp).HasDefaultValueSql("now()"));
        modelBuilder.Entity<CachedUser>(entity => entity.Property(e => e.Discriminator).HasMaxLength(4).IsFixedLength());
        modelBuilder.Entity<CachedGuildUser>(entity => {
            entity.Navigation(e => e.User).AutoInclude();
            entity.HasKey(e => new { e.UserId, e.GuildId });
            entity.Property(e => e.FirstSeenTime).HasDefaultValueSql("now()");
        });
        modelBuilder.Entity<CachedGuildMessage>(entity => entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()"));
    }
}
