using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreekRecruit.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePictureURL3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "pnm_profilepicture_url",
                table: "PNMs",
                newName: "pnm_profilepictureurl");

            migrationBuilder.RenameColumn(
                name: "pnm_profilepicture_url",
                table: "InterestFormSubmissions",
                newName: "pnm_profilepictureurl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "pnm_profilepictureurl",
                table: "PNMs",
                newName: "pnm_profilepicture_url");

            migrationBuilder.RenameColumn(
                name: "pnm_profilepictureurl",
                table: "InterestFormSubmissions",
                newName: "pnm_profilepicture_url");
        }
    }
}
