using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiDocs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentBlockchainFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChainError",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChainStatus",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredOnChainAtUtc",
                table: "Documents",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChainError",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ChainStatus",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "RegisteredOnChainAtUtc",
                table: "Documents");
        }
    }
}
