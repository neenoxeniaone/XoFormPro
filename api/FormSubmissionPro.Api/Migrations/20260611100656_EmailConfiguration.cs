using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FormSubmissionPro.Api.Migrations
{
    /// <inheritdoc />
    public partial class EmailConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Provider = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DefaultFromName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GraphTenantId = table.Column<string>(type: "text", nullable: false),
                    GraphClientId = table.Column<string>(type: "text", nullable: false),
                    GraphClientSecret = table.Column<string>(type: "text", nullable: false),
                    GraphSender = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailConfigurations", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "EmailConfigurations",
                columns: new[] { "Id", "DefaultFromName", "GraphClientId", "GraphClientSecret", "GraphSender", "GraphTenantId", "Provider", "UpdatedAt" },
                values: new object[] { 1, "FormSubmissionPro", "", "", "", "", "console", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailConfigurations");
        }
    }
}
