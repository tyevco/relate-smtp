using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Relate.Smtp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailThreading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InReplyTo",
                table: "Emails",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "References",
                table: "Emails",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ThreadId",
                table: "Emails",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Emails_ThreadId",
                table: "Emails",
                column: "ThreadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Emails_ThreadId",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "InReplyTo",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "References",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "ThreadId",
                table: "Emails");
        }
    }
}
