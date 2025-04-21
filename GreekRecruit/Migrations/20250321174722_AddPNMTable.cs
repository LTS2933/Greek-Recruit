using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreekRecruit.Migrations
{
    /// <inheritdoc />
    public partial class AddPNMTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PNMs",
                columns: table => new
                {
                    pnm_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    pnm_fname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    pnm_lname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    pnm_email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pnm_phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pnm_gpa = table.Column<double>(type: "float", nullable: true),
                    pnm_major = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pnm_schoolyear = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pnm_instagramhandle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pnm_profilepicture = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    pnm_comments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PNMs", x => x.pnm_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PNMs");
        }
    }
}
