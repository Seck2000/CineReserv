using CineReserv.Data;
using CineReserv.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CineReserv.Helpers
{
    /// <summary>
    /// Helper pour la gestion des utilisateurs (vérifications, récupérations, etc.)
    /// </summary>
    public static class UserHelper
    {
        /// <summary>
        /// Vérifie si l'utilisateur connecté est un fournisseur/organisateur.
        /// </summary>
        /// <param name="controller">Le contrôleur ASP.NET Core</param>
        /// <param name="context">Le contexte de base de données</param>
        /// <returns>True si l'utilisateur est un fournisseur, False sinon</returns>
        public static bool IsSupplier(Controller controller, ApplicationDbContext context)
        {
            var userId = controller.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return false;
            }

            var user = context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                return false;
            }
            
            // Vérifier avec trim pour éviter les espaces et comparaison insensible à la casse
            return !string.IsNullOrEmpty(user.TypeUtilisateur) && 
                   user.TypeUtilisateur.Trim().Equals("Fournisseur/Organisateur", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Récupère l'ID de l'utilisateur connecté.
        /// </summary>
        /// <param name="controller">Le contrôleur ASP.NET Core</param>
        /// <returns>L'ID de l'utilisateur ou null si non connecté</returns>
        public static string? GetCurrentUserId(Controller controller)
        {
            return controller.User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}

