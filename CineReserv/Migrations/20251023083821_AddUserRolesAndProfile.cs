using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineReserv.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRolesAndProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateInscription",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEntreprise",
                table: "AspNetUsers",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "EstActif",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NomEntreprise",
                table: "AspNetUsers",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TypeUtilisateur",
                table: "AspNetUsers",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateInscription",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DescriptionEntreprise",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EstActif",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NomEntreprise",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TypeUtilisateur",
                table: "AspNetUsers");
        }
    }
}
