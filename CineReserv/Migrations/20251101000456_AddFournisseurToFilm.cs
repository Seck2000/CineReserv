using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineReserv.Migrations
{
    /// <inheritdoc />
    public partial class AddFournisseurToFilm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Seances_AspNetUsers_FournisseurId",
                table: "Seances");

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
                name: "FK_Seances_AspNetUsers_FournisseurId",
                table: "Seances",
                column: "FournisseurId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Films_AspNetUsers_FournisseurId",
                table: "Films");

            migrationBuilder.DropForeignKey(
                name: "FK_Seances_AspNetUsers_FournisseurId",
                table: "Seances");

            migrationBuilder.DropIndex(
                name: "IX_Films_FournisseurId",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "FournisseurId",
                table: "Films");

            migrationBuilder.AddForeignKey(
                name: "FK_Seances_AspNetUsers_FournisseurId",
                table: "Seances",
                column: "FournisseurId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
