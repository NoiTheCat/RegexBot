using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RegexBot.Data.Migrations
{
    public partial class AddModLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_cache_usersinguild",
                table: "cache_usersinguild");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:mod_log_type", "other,note,warn,timeout,kick,ban");

            migrationBuilder.AddPrimaryKey(
                name: "pk_cache_usersinguild",
                table: "cache_usersinguild",
                columns: new[] { "guild_id", "user_id" });

            migrationBuilder.CreateTable(
                name: "modlogs",
                columns: table => new
                {
                    log_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    guild_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    log_type = table.Column<int>(type: "integer", nullable: false),
                    issued_by = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_modlogs", x => x.log_id);
                    table.ForeignKey(
                        name: "fk_modlogs_cache_usersinguild_user_temp_id",
                        columns: x => new { x.guild_id, x.user_id },
                        principalTable: "cache_usersinguild",
                        principalColumns: new[] { "guild_id", "user_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cache_usersinguild_user_id",
                table: "cache_usersinguild",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_modlogs_guild_id_user_id",
                table: "modlogs",
                columns: new[] { "guild_id", "user_id" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "modlogs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_cache_usersinguild",
                table: "cache_usersinguild");

            migrationBuilder.DropIndex(
                name: "ix_cache_usersinguild_user_id",
                table: "cache_usersinguild");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:mod_log_type", "other,note,warn,timeout,kick,ban");

            migrationBuilder.AddPrimaryKey(
                name: "pk_cache_usersinguild",
                table: "cache_usersinguild",
                columns: new[] { "user_id", "guild_id" });
        }
    }
}
