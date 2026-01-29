using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Relate.Smtp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Theme = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DisplayDensity = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EmailsPerPage = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultSort = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ShowPreview = table.Column<bool>(type: "INTEGER", nullable: false),
                    GroupByDate = table.Column<bool>(type: "INTEGER", nullable: false),
                    DesktopNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailDigest = table.Column<bool>(type: "INTEGER", nullable: false),
                    DigestFrequency = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DigestTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                table: "UserPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPreferences");
        }
    }
}
