using CineReserv.Data;
using Microsoft.EntityFrameworkCore;

namespace CineReserv.Services
{
    public class SessionService : ISessionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SessionService> _logger;

        public SessionService(ApplicationDbContext context, ILogger<SessionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ClearAllSessionsAsync()
        {
            try
            {
                // Supprimer tous les articles de panier de session (utilisateurs non connectés)
                var sessionItems = await _context.PanierItems
                    .Where(p => !string.IsNullOrEmpty(p.SessionId) && string.IsNullOrEmpty(p.UserId))
                    .ToListAsync();

                if (sessionItems.Any())
                {
                    _context.PanierItems.RemoveRange(sessionItems);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Supprimé {Count} articles de panier de session", sessionItems.Count);
                }

                // Note: Les cookies de session ASP.NET Core Identity sont gérés côté client
                // Ils seront automatiquement invalidés au prochain démarrage
                _logger.LogInformation("Sessions vidées avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du vidage des sessions");
            }
        }
    }
}

