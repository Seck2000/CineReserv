using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineReserv.Migrations
{
    /// <inheritdoc />
    public partial class AddFournisseurToSeance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FournisseurId",
                table: "Seances",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Seances_FournisseurId",
                table: "Seances",
                column: "FournisseurId");

            migrationBuilder.AddForeignKey(
                name: "FK_Seances_AspNetUsers_FournisseurId",
                table: "Seances",
                column: "FournisseurId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Seances_AspNetUsers_FournisseurId",
                table: "Seances");

            migrationBuilder.DropIndex(
                name: "IX_Seances_FournisseurId",
                table: "Seances");

            migrationBuilder.DropColumn(
                name: "FournisseurId",
                table: "Seances");
        }
    }
}
