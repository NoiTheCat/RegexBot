﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using RegexBot.Data;

#nullable disable

namespace RegexBot.Data.Migrations
{
    [DbContext(typeof(BotDatabaseContext))]
    partial class BotDatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "mod_log_type", new[] { "other", "note", "warn", "timeout", "kick", "ban" });
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("RegexBot.Data.CachedGuildMessage", b =>
                {
                    b.Property<long>("MessageId")
                        .HasColumnType("bigint")
                        .HasColumnName("message_id");

                    b.Property<List<string>>("AttachmentNames")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("attachment_names");

                    b.Property<long>("AuthorId")
                        .HasColumnType("bigint")
                        .HasColumnName("author_id");

                    b.Property<long>("ChannelId")
                        .HasColumnType("bigint")
                        .HasColumnName("channel_id");

                    b.Property<string>("Content")
                        .HasColumnType("text")
                        .HasColumnName("content");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("now()");

                    b.Property<DateTimeOffset?>("EditedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("edited_at");

                    b.Property<long>("GuildId")
                        .HasColumnType("bigint")
                        .HasColumnName("guild_id");

                    b.HasKey("MessageId")
                        .HasName("pk_cache_guildmessages");

                    b.HasIndex("AuthorId")
                        .HasDatabaseName("ix_cache_guildmessages_author_id");

                    b.ToTable("cache_guildmessages", (string)null);
                });

            modelBuilder.Entity("RegexBot.Data.CachedGuildUser", b =>
                {
                    b.Property<long>("GuildId")
                        .HasColumnType("bigint")
                        .HasColumnName("guild_id");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint")
                        .HasColumnName("user_id");

                    b.Property<DateTimeOffset>("FirstSeenTime")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("first_seen_time")
                        .HasDefaultValueSql("now()");

                    b.Property<DateTimeOffset>("GULastUpdateTime")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("gu_last_update_time");

                    b.Property<string>("Nickname")
                        .HasColumnType("text")
                        .HasColumnName("nickname");

                    b.HasKey("GuildId", "UserId")
                        .HasName("pk_cache_usersinguild");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_cache_usersinguild_user_id");

                    b.ToTable("cache_usersinguild", (string)null);
                });

            modelBuilder.Entity("RegexBot.Data.CachedUser", b =>
                {
                    b.Property<long>("UserId")
                        .HasColumnType("bigint")
                        .HasColumnName("user_id");

                    b.Property<string>("AvatarUrl")
                        .HasColumnType("text")
                        .HasColumnName("avatar_url");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasMaxLength(4)
                        .HasColumnType("character(4)")
                        .HasColumnName("discriminator")
                        .IsFixedLength();

                    b.Property<DateTimeOffset>("ULastUpdateTime")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("u_last_update_time");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("username");

                    b.HasKey("UserId")
                        .HasName("pk_cache_users");

                    b.ToTable("cache_users", (string)null);
                });

            modelBuilder.Entity("RegexBot.Data.ModLogEntry", b =>
                {
                    b.Property<int>("LogId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("log_id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("LogId"));

                    b.Property<long>("GuildId")
                        .HasColumnType("bigint")
                        .HasColumnName("guild_id");

                    b.Property<string>("IssuedBy")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("issued_by");

                    b.Property<int>("LogType")
                        .HasColumnType("integer")
                        .HasColumnName("log_type");

                    b.Property<string>("Message")
                        .HasColumnType("text")
                        .HasColumnName("message");

                    b.Property<DateTimeOffset>("Timestamp")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("timestamp")
                        .HasDefaultValueSql("now()");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint")
                        .HasColumnName("user_id");

                    b.HasKey("LogId")
                        .HasName("pk_modlogs");

                    b.HasIndex("GuildId", "UserId")
                        .HasDatabaseName("ix_modlogs_guild_id_user_id");

                    b.ToTable("modlogs", (string)null);
                });

            modelBuilder.Entity("RegexBot.Data.CachedGuildMessage", b =>
                {
                    b.HasOne("RegexBot.Data.CachedUser", "Author")
                        .WithMany("GuildMessages")
                        .HasForeignKey("AuthorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_cache_guildmessages_cache_users_author_id");

                    b.Navigation("Author");
                });

            modelBuilder.Entity("RegexBot.Data.CachedGuildUser", b =>
                {
                    b.HasOne("RegexBot.Data.CachedUser", "User")
                        .WithMany("Guilds")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_cache_usersinguild_cache_users_user_id");

                    b.Navigation("User");
                });

            modelBuilder.Entity("RegexBot.Data.ModLogEntry", b =>
                {
                    b.HasOne("RegexBot.Data.CachedGuildUser", "User")
                        .WithMany("Logs")
                        .HasForeignKey("GuildId", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_modlogs_cache_usersinguild_user_temp_id");

                    b.Navigation("User");
                });

            modelBuilder.Entity("RegexBot.Data.CachedGuildUser", b =>
                {
                    b.Navigation("Logs");
                });

            modelBuilder.Entity("RegexBot.Data.CachedUser", b =>
                {
                    b.Navigation("GuildMessages");

                    b.Navigation("Guilds");
                });
#pragma warning restore 612, 618
        }
    }
}
