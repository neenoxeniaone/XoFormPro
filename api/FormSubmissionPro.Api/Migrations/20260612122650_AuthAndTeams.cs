using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormSubmissionPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class AuthAndTeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "email_verified",
                table: "fsp_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "fsp_users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "fsp_invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fsp_invitations", x => x.id);
                    table.ForeignKey(
                        name: "fk_fsp_invitations_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "fsp_organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fsp_user_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fsp_user_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_fsp_user_tokens_fsp_users_user_id",
                        column: x => x.user_id,
                        principalTable: "fsp_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fsp_invitations_organization_id",
                table: "fsp_invitations",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_fsp_invitations_token_hash",
                table: "fsp_invitations",
                column: "token_hash");

            migrationBuilder.CreateIndex(
                name: "ix_fsp_user_tokens_token_hash",
                table: "fsp_user_tokens",
                column: "token_hash");

            migrationBuilder.CreateIndex(
                name: "ix_fsp_user_tokens_user_id",
                table: "fsp_user_tokens",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fsp_invitations");

            migrationBuilder.DropTable(
                name: "fsp_user_tokens");

            migrationBuilder.DropColumn(
                name: "email_verified",
                table: "fsp_users");

            migrationBuilder.DropColumn(
                name: "role",
                table: "fsp_users");
        }
    }
}
