using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineReserv.Migrations
{
    /// <inheritdoc />
    public partial class AddSiegeIdsToPanierItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Films_AspNetUsers_FournisseurId",
                table: "Films");

            migrationBuilder.DropForeignKey(
                name: "FK_Sieges_Reservations_ReservationId",
                table: "Sieges");

            migrationBuilder.DropIndex(
                name: "IX_Films_FournisseurId",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "FournisseurId",
                table: "Films");

            migrationBuilder.AddColumn<string>(
                name: "SiegeIds",
                table: "PanierItems",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_Sieges_Reservations_ReservationId",
                table: "Sieges",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sieges_Reservations_ReservationId",
                table: "Sieges");

            migrationBuilder.DropColumn(
                name: "SiegeIds",
                table: "PanierItems");

            migrationBuilder.AddColumn<string>(
                name: "FournisseurId",
                table: "Films",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Films_FournisseurId",
                table: "Films",
                column: "FournisseurId");

            migrationBuilder.AddForeignKey(
                name: "FK_Films_AspNetUsers_FournisseurId",
                table: "Films",
                column: "FournisseurId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sieges_Reservations_ReservationId",
                table: "Sieges",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id");
        }
    }
}
