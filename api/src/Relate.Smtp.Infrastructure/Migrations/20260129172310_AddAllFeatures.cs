using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Relate.Smtp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAllFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add threading columns to Emails
            migrationBuilder.AddColumn<string>(
                name: "InReplyTo",
                table: "Emails",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "References",
                table: "Emails",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ThreadId",
                table: "Emails",
                type: "uuid",
                nullable: true);

            // Add SentByUserId column
            migrationBuilder.AddColumn<Guid>(
                name: "SentByUserId",
                table: "Emails",
                type: "uuid",
                nullable: true);

            // Create Labels table
            migrationBuilder.CreateTable(
                name: "Labels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Labels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Labels_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create EmailLabels table
            migrationBuilder.CreateTable(
                name: "EmailLabels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LabelId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLabels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailLabels_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailLabels_Labels_LabelId",
                        column: x => x.LabelId,
                        principalTable: "Labels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailLabels_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create EmailFilters table
            migrationBuilder.CreateTable(
                name: "EmailFilters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FromContains = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    SubjectContains = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BodyContains = table.Column<string>(type: "text", nullable: true),
                    HasAttachments = table.Column<bool>(type: "boolean", nullable: true),
                    ApplyLabelId = table.Column<Guid>(type: "uuid", nullable: true),
                    MarkAsRead = table.Column<bool>(type: "boolean", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailFilters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailFilters_Labels_ApplyLabelId",
                        column: x => x.ApplyLabelId,
                        principalTable: "Labels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EmailFilters_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create UserPreferences table
            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Theme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "system"),
                    DisplayDensity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "comfortable"),
                    EmailsPerPage = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                    DefaultSort = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "receivedAt-desc"),
                    ShowPreview = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    GroupByDate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DesktopNotifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EmailDigest = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DigestFrequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "daily"),
                    DigestTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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

            // Create PushSubscriptions table
            migrationBuilder.CreateTable(
                name: "PushSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Endpoint = table.Column<string>(type: "text", nullable: false),
                    P256dh = table.Column<string>(type: "text", nullable: false),
                    Auth = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PushSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_Emails_ThreadId",
                table: "Emails",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_SentByUserId",
                table: "Emails",
                column: "SentByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLabels_EmailId_LabelId",
                table: "EmailLabels",
                columns: new[] { "EmailId", "LabelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailLabels_LabelId",
                table: "EmailLabels",
                column: "LabelId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLabels_UserId_LabelId",
                table: "EmailLabels",
                columns: new[] { "UserId", "LabelId" });

            migrationBuilder.CreateIndex(
                name: "IX_Labels_UserId_Name",
                table: "Labels",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailFilters_UserId",
                table: "EmailFilters",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailFilters_ApplyLabelId",
                table: "EmailFilters",
                column: "ApplyLabelId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                table: "UserPreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_Endpoint",
                table: "PushSubscriptions",
                column: "Endpoint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_UserId",
                table: "PushSubscriptions",
                column: "UserId");

            // Add foreign key for SentByUserId
            migrationBuilder.AddForeignKey(
                name: "FK_Emails_Users_SentByUserId",
                table: "Emails",
                column: "SentByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Emails_Users_SentByUserId",
                table: "Emails");

            migrationBuilder.DropTable(name: "PushSubscriptions");
            migrationBuilder.DropTable(name: "UserPreferences");
            migrationBuilder.DropTable(name: "EmailFilters");
            migrationBuilder.DropTable(name: "EmailLabels");
            migrationBuilder.DropTable(name: "Labels");

            migrationBuilder.DropIndex(
                name: "IX_Emails_SentByUserId",
                table: "Emails");

            migrationBuilder.DropIndex(
                name: "IX_Emails_ThreadId",
                table: "Emails");

            migrationBuilder.DropColumn(name: "SentByUserId", table: "Emails");
            migrationBuilder.DropColumn(name: "ThreadId", table: "Emails");
            migrationBuilder.DropColumn(name: "References", table: "Emails");
            migrationBuilder.DropColumn(name: "InReplyTo", table: "Emails");
        }
    }
}
