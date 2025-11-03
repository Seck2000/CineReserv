using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineReserv.Migrations
{
    /// <inheritdoc />
    public partial class FixSalleSiegeRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cette migration corrige seulement la relation entre Siege et Salle
            // La colonne SiegeIds a déjà été créée dans la migration précédente
            // Il n'y a pas de changement de schéma nécessaire, juste une correction de la configuration EF Core
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Pas de changement de schéma à annuler
        }
    }
}
