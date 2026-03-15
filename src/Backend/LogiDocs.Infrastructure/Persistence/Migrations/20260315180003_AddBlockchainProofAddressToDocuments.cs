using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiDocs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockchainProofAddressToDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlockchainProofAddress",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockchainProofAddress",
                table: "Documents");
        }
    }
}
