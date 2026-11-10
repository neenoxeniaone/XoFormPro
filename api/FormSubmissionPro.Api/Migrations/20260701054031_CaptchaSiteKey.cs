using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormSubmissionPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class CaptchaSiteKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "captcha_site_key",
                table: "fsp_forms",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "captcha_site_key",
                table: "fsp_forms");
        }
    }
}
