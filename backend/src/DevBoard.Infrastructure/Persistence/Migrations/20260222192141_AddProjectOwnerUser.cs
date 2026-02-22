using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevBoard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectOwnerUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerUserId_CreatedAt",
                table: "Projects",
                columns: new[] { "OwnerUserId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_OwnerUserId",
                table: "Projects",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_OwnerUserId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_OwnerUserId_CreatedAt",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Projects");
        }
    }
}
