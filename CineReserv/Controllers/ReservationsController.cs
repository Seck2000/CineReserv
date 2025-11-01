using CineReserv.Data;
using CineReserv.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CineReserv.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReservationsController> _logger;

        public ReservationsController(ApplicationDbContext context, ILogger<ReservationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Reservations
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var reservations = await _context.Reservations
                .Include(r => r.Seance)
                    .ThenInclude(s => s.Film)
                .Include(r => r.Seance)
                    .ThenInclude(s => s.Salle)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.DateReservation)
                .ToListAsync();

            // Récupérer les factures associées aux réservations
            var reservationIds = reservations.Select(r => r.Id).ToList();
            var invoices = await _context.Factures
                .Where(i => reservationIds.Contains(i.ReservationId) && i.ClientId == userId)
                .ToListAsync();

            // Créer un dictionnaire pour faciliter l'accès aux factures depuis la vue
            var invoiceIds = invoices.ToDictionary(i => i.ReservationId, i => i.Id);
            ViewBag.InvoiceIds = invoiceIds;

            return View(reservations);
        }

        // GET: Reservations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var reservation = await _context.Reservations
                .Include(r => r.Seance)
                    .ThenInclude(s => s.Film)
                .Include(r => r.Seance)
                    .ThenInclude(s => s.Salle)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reservation == null)
            {
                return NotFound();
            }

            // Récupérer uniquement les sièges réservés pour cette réservation spécifique
            var siegesReserves = await _context.Sieges
                .Where(s => s.ReservationId == reservation.Id && s.UserId == userId)
                .OrderBy(s => s.Rang)
                .ThenBy(s => s.Numero)
                .ToListAsync();

            ViewBag.SiegesReserves = siegesReserves;

            // Chercher une facture liée à cette réservation pour ce client
            var invoice = await _context.Factures.FirstOrDefaultAsync(i => i.ReservationId == reservation.Id && i.ClientId == userId);
            if (invoice != null)
            {
                ViewBag.InvoiceId = invoice.Id;
            }

            return View(reservation);
        }

        // POST: Reservations/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reservation == null)
            {
                return NotFound();
            }

            // Vérifier si la réservation peut être annulée (pas trop proche de la séance)
            var seance = await _context.Seances.FindAsync(reservation.SeanceId);
            if (seance != null && seance.DateHeure <= DateTime.Now.AddHours(2))
            {
                TempData["ErrorMessage"] = "Impossible d'annuler une réservation moins de 2h avant la séance";
                return RedirectToAction(nameof(Index));
            }

            reservation.Statut = "Annulee";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Réservation annulée avec succès";
            return RedirectToAction(nameof(Index));
        }
    }
}
