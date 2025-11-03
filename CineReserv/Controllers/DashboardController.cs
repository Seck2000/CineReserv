using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CineReserv.Data;
using CineReserv.Models;
using System.Security.Claims;

namespace CineReserv.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Dashboard
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);

            if (user == null || user.TypeUtilisateur != "Fournisseur/Organisateur")
            {
                return RedirectToAction("Index", "Home");
            }

            // Si le fournisseur n'a aucune séance assignée, lui assigner automatiquement 
            // toutes les séances non assignées (FournisseurId == null)
            var hasSeances = await _context.Seances
                .AnyAsync(s => s.FournisseurId == userId && s.EstActive);
            
            if (!hasSeances)
            {
                var seancesNonAssignees = await _context.Seances
                    .Where(s => s.FournisseurId == null && s.EstActive)
                    .ToListAsync();
                
                if (seancesNonAssignees.Any())
                {
                    foreach (var seance in seancesNonAssignees)
                    {
                        seance.FournisseurId = userId;
                    }
                    await _context.SaveChangesAsync();
                }
            }

            // Récupérer les statistiques
            var stats = await GetDashboardStats(userId);

            return View(stats);
        }

        private async Task<DashboardStats> GetDashboardStats(string userId)
        {
            // Récupérer uniquement les séances assignées au fournisseur connecté
            var seances = await _context.Seances
                .Include(s => s.Salle)
                .Include(s => s.Film)
                .Where(s => s.EstActive && s.FournisseurId == userId)
                .ToListAsync();

            var seanceIds = seances.Select(s => s.Id).ToList();

            // Récupérer les réservations pour ces séances
            var reservations = await _context.Reservations
                .Include(r => r.Seance)
                    .ThenInclude(s => s.Film)
                .Include(r => r.Seance)
                    .ThenInclude(s => s.Salle)
                .Include(r => r.User)
                .Where(r => seanceIds.Contains(r.SeanceId))
                .ToListAsync();

            // Calculer les statistiques
            var totalRevenus = reservations.Sum(r => r.PrixTotal);
            var totalPlacesVendues = reservations.Sum(r => r.NombrePlaces);
            var totalPlacesDisponibles = seances.Sum(s => s.Salle.NombrePlaces);
            var tauxRemplissage = totalPlacesDisponibles > 0 ? (double)totalPlacesVendues / totalPlacesDisponibles * 100 : 0;
            var clientsActifs = reservations.Select(r => r.UserId).Distinct().Count();

            // Réservations récentes (30 derniers jours)
            var reservationsRecent = reservations
                .Where(r => r.DateReservation >= DateTime.Now.AddDays(-30))
                .OrderByDescending(r => r.DateReservation)
                .Take(10)
                .ToList();

            // Clients les plus actifs (top 10 par nombre de réservations)
            var clientsLesPlusActifs = reservations
                .GroupBy(r => new { r.UserId, r.User })
                .Select(g => new ClientActifStats
                {
                    UserId = g.Key.UserId,
                    NomComplet = $"{g.Key.User?.Prenom} {g.Key.User?.Nom}",
                    Email = g.Key.User?.Email ?? "",
                    NombreReservations = g.Count(),
                    TotalDepense = g.Sum(r => r.PrixTotal),
                    TotalPlaces = g.Sum(r => r.NombrePlaces)
                })
                .OrderByDescending(c => c.NombreReservations)
                .Take(10)
                .ToList();

            return new DashboardStats
            {
                TotalRevenus = totalRevenus,
                TotalPlacesVendues = totalPlacesVendues,
                TauxRemplissage = tauxRemplissage,
                ClientsActifs = clientsActifs,
                NombreFilms = seances.Select(s => s.FilmId).Distinct().Count(),
                NombreSeances = seances.Count,
                ReservationsRecent = reservationsRecent,
                ClientsLesPlusActifs = clientsLesPlusActifs
            };
        }
    }

    public class DashboardStats
    {
        public decimal TotalRevenus { get; set; }
        public int TotalPlacesVendues { get; set; }
        public double TauxRemplissage { get; set; }
        public int ClientsActifs { get; set; }
        public int NombreFilms { get; set; }
        public int NombreSeances { get; set; }
        public List<Reservation> ReservationsRecent { get; set; } = new List<Reservation>();
        public List<ClientActifStats> ClientsLesPlusActifs { get; set; } = new List<ClientActifStats>();
    }

    public class ClientActifStats
    {
        public string UserId { get; set; } = string.Empty;
        public string NomComplet { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int NombreReservations { get; set; }
        public decimal TotalDepense { get; set; }
        public int TotalPlaces { get; set; }
    }
}
