using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegexBot.Data.Migrations
{
    public partial class NewUsernames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "global_name",
                table: "cache_users",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "global_name",
                table: "cache_users");
        }
    }
}
