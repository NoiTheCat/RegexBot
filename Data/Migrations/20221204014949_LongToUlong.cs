using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegexBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class LongToUlong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: manually edited Up and Down - must drop and re-add foreign keys due to altered types
            migrationBuilder.DropForeignKey(
                name: "fk_modlogs_cache_usersinguild_user_temp_id",
                table: "modlogs");
            migrationBuilder.DropForeignKey(
                name: "fk_cache_usersinguild_cache_users_user_id",
                table: "cache_usersinguild");

            migrationBuilder.AlterColumn<decimal>(
                name: "user_id",
                table: "modlogs",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<decimal>(
                name: "guild_id",
                table: "modlogs",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<decimal>(
                name: "user_id",
                table: "cache_usersinguild",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<decimal>(
                name: "guild_id",
                table: "cache_usersinguild",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<decimal>(
                name: "user_id",
                table: "cache_users",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<decimal>(
                name: "guild_id",
                table: "cache_guildmessages",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<decimal>(
                name: "channel_id",
                table: "cache_guildmessages",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<decimal>(
                name: "author_id",
                table: "cache_guildmessages",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<decimal>(
                name: "message_id",
                table: "cache_guildmessages",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "fk_modlogs_cache_usersinguild_user_temp_id",
                table: "modlogs",
                columns: new[] { "guild_id", "user_id" },
                principalTable: "cache_usersinguild",
                principalColumns: new[] { "guild_id", "user_id" },
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "fk_cache_usersinguild_cache_users_user_id",
                table: "cache_usersinguild",
                column: "user_id",
                principalTable: "cache_users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_modlogs_cache_usersinguild_user_temp_id",
                table: "modlogs");
            migrationBuilder.DropForeignKey(
                name: "fk_cache_usersinguild_cache_users_user_id",
                table: "cache_usersinguild");

            migrationBuilder.AlterColumn<long>(
                name: "user_id",
                table: "modlogs",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<long>(
                name: "guild_id",
                table: "modlogs",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<long>(
                name: "user_id",
                table: "cache_usersinguild",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<long>(
                name: "guild_id",
                table: "cache_usersinguild",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<long>(
                name: "user_id",
                table: "cache_users",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<long>(
                name: "guild_id",
                table: "cache_guildmessages",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<long>(
                name: "channel_id",
                table: "cache_guildmessages",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<long>(
                name: "author_id",
                table: "cache_guildmessages",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<long>(
                name: "message_id",
                table: "cache_guildmessages",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AddForeignKey(
                name: "fk_modlogs_cache_usersinguild_user_temp_id",
                table: "modlogs",
                columns: new[] { "guild_id", "user_id" },
                principalTable: "cache_usersinguild",
                principalColumns: new[] { "guild_id", "user_id" },
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "fk_cache_usersinguild_cache_users_user_id",
                table: "cache_usersinguild",
                column: "user_id",
                principalTable: "cache_users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
