using CineReserv.Data;
using CineReserv.Models;
using CineReserv.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CineReserv.Controllers
{
    [Authorize]
    public class PanierController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PanierController> _logger;

        public PanierController(ApplicationDbContext context, ILogger<PanierController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Panier
        public async Task<IActionResult> Index()
        {
            var sessionId = GetSessionId();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var panierItems = await _context.PanierItems
                .Include(p => p.Seance)
                    .ThenInclude(s => s.Film)
                .Include(p => p.Seance)
                    .ThenInclude(s => s.Salle)
                        .ThenInclude(s => s.Sieges)
                .Include(p => p.CategorieAge)
                .Where(p => (userId != null && p.UserId == userId) || (userId == null && p.SessionId == sessionId))
                .ToListAsync();

            return View(panierItems);
        }

        // POST: Panier/Ajouter
        [HttpPost]
        public async Task<IActionResult> Ajouter(int seanceId, int nombrePlaces)
        {
            var seance = await _context.Seances
                .Include(s => s.Film)
                .Include(s => s.Salle)
                .FirstOrDefaultAsync(s => s.Id == seanceId);

            if (seance == null)
            {
                return NotFound();
            }

            var sessionId = GetSessionId();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Vérifier si l'item existe déjà dans le panier
            var existingItem = await _context.PanierItems
                .FirstOrDefaultAsync(p => p.SeanceId == seanceId && 
                    ((userId != null && p.UserId == userId) || (userId == null && p.SessionId == sessionId)));

            if (existingItem != null)
            {
                existingItem.NombrePlaces += nombrePlaces;
            }
            else
            {
                var panierItem = new PanierItem
                {
                    SeanceId = seanceId,
                    SessionId = sessionId,
                    UserId = userId,
                    NombrePlaces = nombrePlaces,
                    PrixUnitaire = seance.Prix
                };

                _context.PanierItems.Add(panierItem);
            }

            await _context.SaveChangesAsync();

            // Redirection silencieuse sans message
            return RedirectToAction(nameof(Index));
        }

        // POST: Panier/Modifier
        [HttpPost]
        public async Task<IActionResult> Modifier(int id, int nombrePlaces)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessionId = GetSessionId();

            var panierItem = await _context.PanierItems
                .FirstOrDefaultAsync(p => p.Id == id && 
                    ((userId != null && p.UserId == userId) || (userId == null && p.SessionId == sessionId)));

            if (panierItem == null)
            {
                TempData["ErrorMessage"] = "Article non trouvé ou accès refusé";
                return RedirectToAction(nameof(Index));
            }

            if (nombrePlaces <= 0)
            {
                _context.PanierItems.Remove(panierItem);
            }
            else
            {
                panierItem.NombrePlaces = nombrePlaces;
                panierItem.Quantite = nombrePlaces; // Synchroniser Quantite avec NombrePlaces pour le calcul du prix
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Panier/Supprimer
        [HttpPost]
        public async Task<IActionResult> Supprimer(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessionId = GetSessionId();

            var panierItem = await _context.PanierItems
                .FirstOrDefaultAsync(p => p.Id == id && 
                    ((userId != null && p.UserId == userId) || (userId == null && p.SessionId == sessionId)));

            if (panierItem != null)
            {
                // Libérer les sièges réservés si l'item contient des sièges
                if (!string.IsNullOrEmpty(panierItem.SiegeIds))
                {
                    var siegeIds = SiegeHelper.ParseSiegeIds(panierItem.SiegeIds);
                    var sieges = await SiegeHelper.GetSiegesByIdsAsync(_context, siegeIds);
                    sieges = sieges.Where(s => s.UserId == userId).ToList();
                    
                    SiegeHelper.LibererSieges(sieges);
                    await _context.SaveChangesAsync();
                }
                
                _context.PanierItems.Remove(panierItem);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Panier/Vider
        [HttpPost]
        public async Task<IActionResult> Vider()
        {
            var sessionId = GetSessionId();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var panierItems = await _context.PanierItems
                .Where(p => (userId != null && p.UserId == userId) || (userId == null && p.SessionId == sessionId))
                .ToListAsync();

            // Libérer tous les sièges réservés
            var allSiegeIds = panierItems
                .Where(p => !string.IsNullOrEmpty(p.SiegeIds))
                .SelectMany(p => SiegeHelper.ParseSiegeIds(p.SiegeIds))
                .Distinct()
                .ToList();
            
            if (allSiegeIds.Any() && userId != null)
            {
                var sieges = await SiegeHelper.GetSiegesByIdsAsync(_context, allSiegeIds);
                sieges = sieges.Where(s => s.UserId == userId).ToList();
                
                SiegeHelper.LibererSieges(sieges);
                await _context.SaveChangesAsync();
            }

            _context.PanierItems.RemoveRange(panierItems);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private string GetSessionId()
        {
            if (HttpContext.Session.GetString("SessionId") == null)
            {
                HttpContext.Session.SetString("SessionId", Guid.NewGuid().ToString());
            }
            return HttpContext.Session.GetString("SessionId")!;
        }
    }
}
