using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreekRecruit.Migrations
{
    /// <inheritdoc />
    public partial class AddInterestFormsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterestForms",
                columns: table => new
                {
                    form_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    organization_id = table.Column<int>(type: "int", nullable: false),
                    pnm_fname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    pnm_lname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    pnm_email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    pnm_phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    pnm_schoolyear = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    pnm_major = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pnm_instagramhandle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_submitted = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterestForms", x => x.form_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterestForms");
        }
    }
}
