using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreekRecruit.Migrations
{
    /// <inheritdoc />
    public partial class AddPointsCodetable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PointsCategories",
                columns: table => new
                {
                    PointsCategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActionName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PointsValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsCategories", x => x.PointsCategoryID);
                });

            migrationBuilder.CreateTable(
                name: "UserPointLogs",
                columns: table => new
                {
                    UserPointLogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    PointsCategoryID = table.Column<int>(type: "int", nullable: false),
                    PointsAwarded = table.Column<int>(type: "int", nullable: false),
                    DateAwarded = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPointLogs", x => x.UserPointLogID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PointsCategories");

            migrationBuilder.DropTable(
                name: "UserPointLogs");
        }
    }
}
