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

        public async Task<IActionResult> Index(string? genre, string? recherche)
        {
            // Récupérer les films pour la page d'accueil
            var query = _context.Films
                .Where(f => f.EstActif)
                .AsQueryable();

            // Filtre par genre
            if (!string.IsNullOrEmpty(genre))
            {
                var genreNormalized = genre.Trim();
                query = query.Where(f => f.Genre == genreNormalized);
            }

            // Recherche par titre ou description
            if (!string.IsNullOrEmpty(recherche))
            {
                query = query.Where(f => f.Titre.Contains(recherche) || f.Description.Contains(recherche));
            }

            var tousLesFilms = await query
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
            ViewBag.GenreSelectionne = genre;
            ViewBag.Recherche = recherche;

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
    }
}
