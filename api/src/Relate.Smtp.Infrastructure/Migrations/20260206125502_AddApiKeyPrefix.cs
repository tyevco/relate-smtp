using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable IDE0161 // Convert to file-scoped namespace - EF Core generated migration
#pragma warning disable CA1861 // Prefer static readonly fields - EF Core generated migration

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
