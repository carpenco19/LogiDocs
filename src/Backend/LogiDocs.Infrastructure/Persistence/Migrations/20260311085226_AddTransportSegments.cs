using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiDocs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransportSegments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransportSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNo = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OperatorName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransportSegments_Transports_TransportId",
                        column: x => x.TransportId,
                        principalTable: "Transports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransportSegments_TransportId_OrderNo",
                table: "TransportSegments",
                columns: new[] { "TransportId", "OrderNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransportSegments");
        }
    }
}
