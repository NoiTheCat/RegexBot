using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegexBot.Data.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cache_users",
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
                    table.PrimaryKey("pk_cache_users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "cache_guildmessages",
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
                    table.PrimaryKey("pk_cache_guildmessages", x => x.message_id);
                    table.ForeignKey(
                        name: "fk_cache_guildmessages_cache_users_author_id",
                        column: x => x.author_id,
                        principalTable: "cache_users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cache_usersinguild",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    gu_last_update_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    first_seen_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nickname = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cache_usersinguild", x => new { x.user_id, x.guild_id });
                    table.ForeignKey(
                        name: "fk_cache_usersinguild_cache_users_user_id",
                        column: x => x.user_id,
                        principalTable: "cache_users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cache_guildmessages_author_id",
                table: "cache_guildmessages",
                column: "author_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cache_guildmessages");

            migrationBuilder.DropTable(
                name: "cache_usersinguild");

            migrationBuilder.DropTable(
                name: "cache_users");
        }
    }
}
