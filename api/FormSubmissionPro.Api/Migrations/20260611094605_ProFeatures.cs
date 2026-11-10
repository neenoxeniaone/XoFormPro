using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormSubmissionPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class ProFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AutoresponderBody",
                table: "Forms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoresponderEnabled",
                table: "Forms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AutoresponderSubject",
                table: "Forms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CaptchaProvider",
                table: "Forms",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CaptchaSecret",
                table: "Forms",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Webhooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Webhooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Webhooks_Forms_FormId",
                        column: x => x.FormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Webhooks_FormId",
                table: "Webhooks",
                column: "FormId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Webhooks");

            migrationBuilder.DropColumn(
                name: "AutoresponderBody",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "AutoresponderEnabled",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "AutoresponderSubject",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "CaptchaProvider",
                table: "Forms");

            migrationBuilder.DropColumn(
                name: "CaptchaSecret",
                table: "Forms");
        }
    }
}
