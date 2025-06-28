using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreekRecruit.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnsToInterestFormSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pnm_clubinvolvement",
                table: "InterestFormSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pnm_hometown",
                table: "InterestFormSubmissions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pnm_clubinvolvement",
                table: "InterestFormSubmissions");

            migrationBuilder.DropColumn(
                name: "pnm_hometown",
                table: "InterestFormSubmissions");
        }
    }
}
