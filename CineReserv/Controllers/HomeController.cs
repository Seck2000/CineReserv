using System.Diagnostics;
using CineReserv.Models;
using CineReserv.Data;
using CineReserv.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CineReserv.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IApiService _apiService;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IApiService apiService)
        {
            _logger = logger;
            _context = context;
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            // Récupérer TOUS les films pour la page d'accueil
            var tousLesFilms = await _context.Films
                .Where(f => f.EstActif)
                .OrderByDescending(f => f.DateSortie)
                .ToListAsync();

            // Récupérer les genres pour les filtres
            var genres = await _context.Films
                .Where(f => f.EstActif)
                .Select(f => f.Genre)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            ViewBag.TousLesFilms = tousLesFilms;
            ViewBag.Genres = genres;


            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public new IActionResult NotFound()
        {
            return View("NotFound");
        }

        // Action pour forcer le peuplement (utile pour le développement)
        public async Task<IActionResult> ForceSeed()
        {
            try
            {
                await _apiService.ForceSeedDatabaseAsync();
                TempData["SuccessMessage"] = "Base de données re-peuplée avec succès !";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors du re-peuplement : {ex.Message}";
            }
            
            return RedirectToAction("Index");
        }

        // Action pour vérifier le type d'utilisateur
        public async Task<IActionResult> CheckUserType()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                
                if (user != null)
                {
                    TempData["InfoMessage"] = $"Utilisateur: {user.Prenom} {user.Nom} - Type: {user.TypeUtilisateur}";
                }
                else
                {
                    TempData["ErrorMessage"] = "Utilisateur non trouvé";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Aucun utilisateur connecté";
            }
            
            return RedirectToAction("Index");
        }

        // Action pour forcer la synchronisation des données
        public async Task<IActionResult> RefreshData()
        {
            try
            {
                // Forcer le rechargement des données depuis l'API
                await _apiService.ForceSeedDatabaseAsync();
                TempData["SuccessMessage"] = "Données synchronisées avec succès !";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Erreur lors de la synchronisation : {ex.Message}";
            }
            
            return RedirectToAction("Index");
        }
    }
}
