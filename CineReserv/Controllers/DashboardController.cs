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

            // Récupérer les statistiques
            var stats = await GetDashboardStats(userId);

            return View(stats);
        }

        private async Task<DashboardStats> GetDashboardStats(string userId)
        {
            // Pour l'instant, on récupère toutes les séances (simulation)
            // Dans une vraie application, on associerait les films aux fournisseurs
            var seances = await _context.Seances
                .Include(s => s.Salle)
                .Include(s => s.Film)
                .ToListAsync();

            var seanceIds = seances.Select(s => s.Id).ToList();

            // Récupérer les réservations
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

            return new DashboardStats
            {
                TotalRevenus = totalRevenus,
                TotalPlacesVendues = totalPlacesVendues,
                TauxRemplissage = tauxRemplissage,
                ClientsActifs = clientsActifs,
                NombreFilms = seances.Select(s => s.FilmId).Distinct().Count(),
                NombreSeances = seances.Count,
                ReservationsRecent = reservationsRecent
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
    }
}
