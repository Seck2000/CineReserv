using CineReserv.Data;
using CineReserv.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CineReserv.Controllers
{
    [Authorize]
    public class GestionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GestionController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsSupplier()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return false;
            
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            return user?.TypeUtilisateur == "Fournisseur/Organisateur";
        }

        // GET: Gestion/Films
        public async Task<IActionResult> Films()
        {
            if (!IsSupplier())
            {
                TempData["ErrorMessage"] = "Accès refusé. Seuls les fournisseurs peuvent accéder à cette page.";
                return RedirectToAction("Index", "Home");
            }

            var films = await _context.Films
                .Include(f => f.Seances)
                    .ThenInclude(s => s.Salle)
                .Where(f => f.EstActif)
                .OrderBy(f => f.Titre)
                .ToListAsync();

            return View(films);
        }

        // GET: Gestion/Seances
        public async Task<IActionResult> Seances()
        {
            if (!IsSupplier())
            {
                TempData["ErrorMessage"] = "Accès refusé. Seuls les fournisseurs peuvent accéder à cette page.";
                return RedirectToAction("Index", "Home");
            }

            var seances = await _context.Seances
                .Include(s => s.Film)
                .Include(s => s.Salle)
                .Where(s => s.EstActive)
                .OrderBy(s => s.DateHeure)
                .ToListAsync();

            return View(seances);
        }

        // GET: Gestion/CreateFilm
        public IActionResult CreateFilm()
        {
            if (!IsSupplier())
            {
                TempData["ErrorMessage"] = "Accès refusé. Seuls les fournisseurs peuvent accéder à cette page.";
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Gestion/CreateFilm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFilm(Film film)
        {
            if (!IsSupplier())
            {
                TempData["ErrorMessage"] = "Accès refusé. Seuls les fournisseurs peuvent accéder à cette page.";
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                film.EstActif = true;
                _context.Add(film);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Film créé avec succès !";
                return RedirectToAction(nameof(Films));
            }
            return View(film);
        }

        // GET: Gestion/EditFilm/5
        public async Task<IActionResult> EditFilm(int? id)
        {
            if (!IsSupplier())
            {
                TempData["ErrorMessage"] = "Accès refusé. Seuls les fournisseurs peuvent accéder à cette page.";
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var film = await _context.Films.FindAsync(id);
            if (film == null)
            {
                return NotFound();
            }
            return View(film);
        }

        // POST: Gestion/EditFilm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFilm(int id, Film film)
        {
            if (!IsSupplier())
            {
                TempData["ErrorMessage"] = "Accès refusé. Seuls les fournisseurs peuvent accéder à cette page.";
                return RedirectToAction("Index", "Home");
            }

            if (id != film.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(film);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Film modifié avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FilmExists(film.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Films));
            }
            return View(film);
        }

        // GET: Gestion/DeleteFilm/5
        public async Task<IActionResult> DeleteFilm(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var film = await _context.Films
                .FirstOrDefaultAsync(m => m.Id == id);
            if (film == null)
            {
                return NotFound();
            }

            return View(film);
        }

        // POST: Gestion/DeleteFilm/5
        [HttpPost, ActionName("DeleteFilm")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFilmConfirmed(int id)
        {
            var film = await _context.Films.FindAsync(id);
            if (film != null)
            {
                film.EstActif = false; // Soft delete
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Film supprimé avec succès !";
            }

            return RedirectToAction(nameof(Films));
        }

        // GET: Gestion/CreateSeance
        public async Task<IActionResult> CreateSeance()
        {
            if (!IsSupplier())
            {
                TempData["ErrorMessage"] = "Accès refusé. Seuls les fournisseurs peuvent accéder à cette page.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Films = await _context.Films.Where(f => f.EstActif).ToListAsync();
            ViewBag.Salles = await _context.Salles.ToListAsync();
            return View();
        }

        // POST: Gestion/CreateSeance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSeance(Seance seance)
        {
            if (!IsSupplier())
            {
                TempData["ErrorMessage"] = "Accès refusé. Seuls les fournisseurs peuvent accéder à cette page.";
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                seance.EstActive = true;
                _context.Add(seance);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Séance créée avec succès !";
                return RedirectToAction(nameof(Seances));
            }

            ViewBag.Films = await _context.Films.Where(f => f.EstActif).ToListAsync();
            ViewBag.Salles = await _context.Salles.ToListAsync();
            return View(seance);
        }

        // GET: Gestion/EditSeance/5
        public async Task<IActionResult> EditSeance(int? id)
        {
            if (!IsSupplier())
            {
                TempData["ErrorMessage"] = "Accès refusé. Seuls les fournisseurs peuvent accéder à cette page.";
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var seance = await _context.Seances
                .Include(s => s.Film)
                .Include(s => s.Salle)
                .FirstOrDefaultAsync(s => s.Id == id);
            
            if (seance == null)
            {
                return NotFound();
            }

            ViewBag.Films = await _context.Films.Where(f => f.EstActif).ToListAsync();
            ViewBag.Salles = await _context.Salles.ToListAsync();
            return View(seance);
        }

        // POST: Gestion/EditSeance/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSeance(int id, Seance seance)
        {
            if (!IsSupplier())
            {
                TempData["ErrorMessage"] = "Accès refusé. Seuls les fournisseurs peuvent accéder à cette page.";
                return RedirectToAction("Index", "Home");
            }

            if (id != seance.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(seance);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Séance modifiée avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SeanceExists(seance.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Seances));
            }

            ViewBag.Films = await _context.Films.Where(f => f.EstActif).ToListAsync();
            ViewBag.Salles = await _context.Salles.ToListAsync();
            return View(seance);
        }

        // GET: Gestion/DeleteSeance/5
        public async Task<IActionResult> DeleteSeance(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var seance = await _context.Seances
                .Include(s => s.Film)
                .Include(s => s.Salle)
                .FirstOrDefaultAsync(s => s.Id == id);
            
            if (seance == null)
            {
                return NotFound();
            }

            return View(seance);
        }

        // POST: Gestion/DeleteSeance/5
        [HttpPost, ActionName("DeleteSeance")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSeanceConfirmed(int id)
        {
            var seance = await _context.Seances.FindAsync(id);
            if (seance != null)
            {
                seance.EstActive = false; // Soft delete
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Séance supprimée avec succès !";
            }

            return RedirectToAction(nameof(Seances));
        }

        private bool FilmExists(int id)
        {
            return _context.Films.Any(e => e.Id == id);
        }

        private bool SeanceExists(int id)
        {
            return _context.Seances.Any(e => e.Id == id);
        }
    }
}
