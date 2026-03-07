using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevBoard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectGitHubInstallationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitHubTokenEncrypted",
                table: "Projects");

            migrationBuilder.AddColumn<long>(
                name: "GitHubInstallationId",
                table: "Projects",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitHubInstallationId",
                table: "Projects");

            migrationBuilder.AddColumn<string>(
                name: "GitHubTokenEncrypted",
                table: "Projects",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");
        }
    }
}
