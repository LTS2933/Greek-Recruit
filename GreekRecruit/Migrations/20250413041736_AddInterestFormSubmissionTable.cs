using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreekRecruit.Migrations
{
    /// <inheritdoc />
    public partial class AddInterestFormSubmissionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pnm_email",
                table: "InterestForms");

            migrationBuilder.DropColumn(
                name: "pnm_fname",
                table: "InterestForms");

            migrationBuilder.DropColumn(
                name: "pnm_instagramhandle",
                table: "InterestForms");

            migrationBuilder.DropColumn(
                name: "pnm_lname",
                table: "InterestForms");

            migrationBuilder.DropColumn(
                name: "pnm_major",
                table: "InterestForms");

            migrationBuilder.DropColumn(
                name: "pnm_phone",
                table: "InterestForms");

            migrationBuilder.DropColumn(
                name: "pnm_schoolyear",
                table: "InterestForms");

            migrationBuilder.RenameColumn(
                name: "date_submitted",
                table: "InterestForms",
                newName: "date_created");

            migrationBuilder.CreateTable(
                name: "InterestFormSubmissions",
                columns: table => new
                {
                    submission_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    form_id = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_InterestFormSubmissions", x => x.submission_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterestFormSubmissions");

            migrationBuilder.RenameColumn(
                name: "date_created",
                table: "InterestForms",
                newName: "date_submitted");

            migrationBuilder.AddColumn<string>(
                name: "pnm_email",
                table: "InterestForms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "pnm_fname",
                table: "InterestForms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "pnm_instagramhandle",
                table: "InterestForms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pnm_lname",
                table: "InterestForms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "pnm_major",
                table: "InterestForms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pnm_phone",
                table: "InterestForms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "pnm_schoolyear",
                table: "InterestForms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
