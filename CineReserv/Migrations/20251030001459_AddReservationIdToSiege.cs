using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineReserv.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationIdToSiege : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReservationId",
                table: "Sieges",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sieges_ReservationId",
                table: "Sieges",
                column: "ReservationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sieges_Reservations_ReservationId",
                table: "Sieges",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sieges_Reservations_ReservationId",
                table: "Sieges");

            migrationBuilder.DropIndex(
                name: "IX_Sieges_ReservationId",
                table: "Sieges");

            migrationBuilder.DropColumn(
                name: "ReservationId",
                table: "Sieges");
        }
    }
}
