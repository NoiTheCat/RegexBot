using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RegexBot.Data.Migrations
{
    public partial class InitialEFSetup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cache_user",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    u_last_update_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    username = table.Column<string>(type: "text", nullable: false),
                    discriminator = table.Column<string>(type: "character(4)", fixedLength: true, maxLength: 4, nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cache_user", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "guild_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    source = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guild_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cache_messages",
                columns: table => new
                {
                    message_id = table.Column<long>(type: "bigint", nullable: false),
                    author_id = table.Column<long>(type: "bigint", nullable: false),
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    channel_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    edited_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attachment_names = table.Column<List<string>>(type: "text[]", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cache_messages", x => x.message_id);
                    table.ForeignKey(
                        name: "fk_cache_messages_cache_user_author_id",
                        column: x => x.author_id,
                        principalTable: "cache_user",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cache_userguild",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    gu_last_update_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    first_seen_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    nickname = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cache_userguild", x => new { x.user_id, x.guild_id });
                    table.ForeignKey(
                        name: "fk_cache_userguild_cache_user_user_id",
                        column: x => x.user_id,
                        principalTable: "cache_user",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cache_messages_author_id",
                table: "cache_messages",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_guild_log_guild_id",
                table: "guild_log",
                column: "guild_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cache_messages");

            migrationBuilder.DropTable(
                name: "cache_userguild");

            migrationBuilder.DropTable(
                name: "guild_log");

            migrationBuilder.DropTable(
                name: "cache_user");
        }
    }
}
