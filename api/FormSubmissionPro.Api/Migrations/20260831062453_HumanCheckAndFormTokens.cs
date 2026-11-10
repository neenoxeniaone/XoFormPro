using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormSubmissionPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class HumanCheckAndFormTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "captcha_auto_inject",
                table: "fsp_forms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "require_human_check",
                table: "fsp_forms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "require_token",
                table: "fsp_forms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "fsp_form_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    origin = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fsp_form_tokens", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fsp_form_tokens_expires_at",
                table: "fsp_form_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_fsp_form_tokens_form_id",
                table: "fsp_form_tokens",
                column: "form_id");

            // EF stamps existing rows with the CLR default (false); the property defaults to
            // true, so bring them into line — otherwise auto-inject would be on for new forms
            // and off for every form that already exists.
            migrationBuilder.Sql("UPDATE fsp_forms SET captcha_auto_inject = true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fsp_form_tokens");

            migrationBuilder.DropColumn(
                name: "captcha_auto_inject",
                table: "fsp_forms");

            migrationBuilder.DropColumn(
                name: "require_human_check",
                table: "fsp_forms");

            migrationBuilder.DropColumn(
                name: "require_token",
                table: "fsp_forms");
        }
    }
}
