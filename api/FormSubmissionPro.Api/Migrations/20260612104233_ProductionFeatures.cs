using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormSubmissionPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class ProductionFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_read",
                table: "fsp_submissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_starred",
                table: "fsp_submissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "search_text",
                table: "fsp_submissions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "email_intro",
                table: "fsp_forms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "success_message",
                table: "fsp_forms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "success_title",
                table: "fsp_forms",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "fsp_webhook_deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    webhook_id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    response_status = table.Column<int>(type: "integer", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fsp_webhook_deliveries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fsp_webhook_deliveries_form_id",
                table: "fsp_webhook_deliveries",
                column: "form_id");

            migrationBuilder.CreateIndex(
                name: "ix_fsp_webhook_deliveries_status_next_attempt_at",
                table: "fsp_webhook_deliveries",
                columns: new[] { "status", "next_attempt_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fsp_webhook_deliveries");

            migrationBuilder.DropColumn(
                name: "is_read",
                table: "fsp_submissions");

            migrationBuilder.DropColumn(
                name: "is_starred",
                table: "fsp_submissions");

            migrationBuilder.DropColumn(
                name: "search_text",
                table: "fsp_submissions");

            migrationBuilder.DropColumn(
                name: "email_intro",
                table: "fsp_forms");

            migrationBuilder.DropColumn(
                name: "success_message",
                table: "fsp_forms");

            migrationBuilder.DropColumn(
                name: "success_title",
                table: "fsp_forms");
        }
    }
}
