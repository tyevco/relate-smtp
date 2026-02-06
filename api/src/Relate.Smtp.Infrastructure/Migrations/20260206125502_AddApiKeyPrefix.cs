using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Relate.Smtp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeyPrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KeyPrefix",
                table: "SmtpApiKeys",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SmtpApiKeys_KeyPrefix_RevokedAt",
                table: "SmtpApiKeys",
                columns: new[] { "KeyPrefix", "RevokedAt" },
                filter: "\"RevokedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SmtpApiKeys_KeyPrefix_RevokedAt",
                table: "SmtpApiKeys");

            migrationBuilder.DropColumn(
                name: "KeyPrefix",
                table: "SmtpApiKeys");
        }
    }
}
