using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreekRecruit.Migrations
{
    /// <inheritdoc />
    public partial class AddVotingToSqlDataContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PNMVoteSessions",
                columns: table => new
                {
                    vote_session_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    pnm_id = table.Column<int>(type: "int", nullable: false),
                    session_open_dt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    session_close_dt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    yes_count = table.Column<int>(type: "int", nullable: false),
                    no_count = table.Column<int>(type: "int", nullable: false),
                    voting_open_yn = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PNMVoteSessions", x => x.vote_session_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PNMVoteSessions");
        }
    }
}
