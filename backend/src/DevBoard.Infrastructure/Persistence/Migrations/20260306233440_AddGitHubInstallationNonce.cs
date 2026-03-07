using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevBoard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGitHubInstallationNonce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GitHubInstallationNonces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nonce = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitHubInstallationNonces", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GitHubInstallationNonces_ExpiresAt",
                table: "GitHubInstallationNonces",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_GitHubInstallationNonces_Nonce",
                table: "GitHubInstallationNonces",
                column: "Nonce",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GitHubInstallationNonces");
        }
    }
}
