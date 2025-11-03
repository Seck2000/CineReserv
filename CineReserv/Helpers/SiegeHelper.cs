using CineReserv.Data;
using CineReserv.Models;
using Microsoft.EntityFrameworkCore;

namespace CineReserv.Helpers
{
    /// <summary>
    /// Helper pour la gestion des sièges (parsing, libération, etc.)
    /// </summary>
    public static class SiegeHelper
    {
        /// <summary>
        /// Convertit une chaîne de sièges (séparés par des virgules) en liste d'entiers.
        /// </summary>
        /// <param name="siegeIds">Chaîne contenant les IDs des sièges séparés par des virgules (ex: "1,2,3")</param>
        /// <returns>Liste des IDs des sièges sous forme d'entiers</returns>
        public static List<int> ParseSiegeIds(string? siegeIds)
        {
            if (string.IsNullOrWhiteSpace(siegeIds))
            {
                return new List<int>();
            }

            return siegeIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.TryParse(id.Trim(), out var parsedId) ? parsedId : 0)
                .Where(id => id > 0)
                .ToList();
        }

        /// <summary>
        /// Libère un ensemble de sièges (les marque comme disponibles).
        /// </summary>
        /// <param name="sieges">Liste des sièges à libérer</param>
        public static void LibererSieges(List<Siege> sieges)
        {
            foreach (var siege in sieges)
            {
                siege.EstReserve = false;
                siege.UserId = null;
                siege.ReservationId = null;
                siege.DateReservation = null;
            }
        }

        /// <summary>
        /// Récupère les sièges depuis la base de données basés sur leurs IDs.
        /// </summary>
        /// <param name="context">Le contexte de base de données</param>
        /// <param name="siegeIds">Liste des IDs des sièges à récupérer</param>
        /// <returns>Liste des sièges correspondants</returns>
        public static async Task<List<Siege>> GetSiegesByIdsAsync(ApplicationDbContext context, List<int> siegeIds)
        {
            if (siegeIds == null || !siegeIds.Any())
            {
                return new List<Siege>();
            }

            return await context.Sieges
                .Where(s => siegeIds.Contains(s.Id))
                .ToListAsync();
        }

        /// <summary>
        /// Vérifie si des sièges sont déjà réservés par un autre utilisateur.
        /// </summary>
        /// <param name="sieges">Liste des sièges à vérifier</param>
        /// <param name="currentUserId">ID de l'utilisateur actuel</param>
        /// <returns>Liste des sièges déjà réservés par un autre utilisateur</returns>
        public static List<Siege> GetSiegesDejaReserves(List<Siege> sieges, string? currentUserId)
        {
            return sieges.Where(s => 
                s.EstOccupe || 
                (s.EstReserve && s.UserId != currentUserId && s.ReservationId != null))
                .ToList();
        }
    }
}

