using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreekRecruit.Migrations
{
    /// <inheritdoc />
    public partial class AddGPAMembersKnownToInterestForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "pnm_gpa",
                table: "InterestFormSubmissions",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pnm_membersknown",
                table: "InterestFormSubmissions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pnm_gpa",
                table: "InterestFormSubmissions");

            migrationBuilder.DropColumn(
                name: "pnm_membersknown",
                table: "InterestFormSubmissions");
        }
    }
}
