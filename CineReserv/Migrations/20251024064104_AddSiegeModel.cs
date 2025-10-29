using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineReserv.Migrations
{
    /// <inheritdoc />
    public partial class AddSiegeModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategorieAgeId",
                table: "PanierItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Quantite",
                table: "PanierItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Sieges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SalleId = table.Column<int>(type: "int", nullable: false),
                    Rang = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EstOccupe = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EstReserve = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateReservation = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sieges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sieges_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Sieges_Salles_SalleId",
                        column: x => x.SalleId,
                        principalTable: "Salles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PanierItems_CategorieAgeId",
                table: "PanierItems",
                column: "CategorieAgeId");

            migrationBuilder.CreateIndex(
                name: "IX_Sieges_SalleId",
                table: "Sieges",
                column: "SalleId");

            migrationBuilder.CreateIndex(
                name: "IX_Sieges_UserId",
                table: "Sieges",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PanierItems_CategoriesAge_CategorieAgeId",
                table: "PanierItems",
                column: "CategorieAgeId",
                principalTable: "CategoriesAge",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PanierItems_CategoriesAge_CategorieAgeId",
                table: "PanierItems");

            migrationBuilder.DropTable(
                name: "Sieges");

            migrationBuilder.DropIndex(
                name: "IX_PanierItems_CategorieAgeId",
                table: "PanierItems");

            migrationBuilder.DropColumn(
                name: "CategorieAgeId",
                table: "PanierItems");

            migrationBuilder.DropColumn(
                name: "Quantite",
                table: "PanierItems");
        }
    }
}
