using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreekRecruit.Migrations
{
    /// <inheritdoc />
    public partial class AddHaveTextedColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "have_texted",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "have_texted",
                table: "Users");
        }
    }
}
