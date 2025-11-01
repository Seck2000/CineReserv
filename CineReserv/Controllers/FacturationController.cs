using CineReserv.Data;
using CineReserv.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CineReserv.Controllers
{
    [Authorize]
    public class FacturationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FacturationController(ApplicationDbContext context)
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

        // GET: Facturation/Index
        public async Task<IActionResult> Index()
        {
            if (!IsSupplier())
            {
                TempData["ErrorMessage"] = "Accès refusé. Seuls les fournisseurs peuvent accéder à cette page.";
                return RedirectToAction("Index", "Home");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Pour les besoins du TP: si aucune facture n'est créée, on projette les réservations
            // comme des "factures" en mémoire afin d'afficher quelque chose au fournisseur.
            var factures = await _context.Factures
                .Include(i => i.Reservation)
                    .ThenInclude(r => r.Seance)
                        .ThenInclude(s => s.Film)
                .Include(i => i.Client)
                .Where(i => i.FournisseurId == userId)
                .OrderByDescending(i => i.DateFacture)
                .ToListAsync();

            if (!factures.Any())
            {
                // Récupérer uniquement les séances du fournisseur connecté
                var seances = await _context.Seances
                    .Include(s => s.Film)
                    .Include(s => s.Salle)
                    .Where(s => s.FournisseurId == userId && s.EstActive)
                    .ToListAsync();

                var seanceIds = seances.Select(s => s.Id).ToList();

                // Récupérer uniquement les réservations pour ces séances
                var reservations = await _context.Reservations
                    .Include(r => r.Seance)
                        .ThenInclude(s => s.Film)
                    .Include(r => r.User)
                    .Where(r => seanceIds.Contains(r.SeanceId))
                    .OrderByDescending(r => r.DateReservation)
                    .ToListAsync();

                factures = reservations.Select(r => new Facture
                {
                    Id = 0, // non persisté
                    NumeroFacture = string.IsNullOrWhiteSpace(r.NumeroReservation) ? $"FAKE-{r.Id}" : r.NumeroReservation,
                    ReservationId = r.Id,
                    Reservation = r,
                    ClientId = r.UserId,
                    Client = r.User,
                    FournisseurId = userId!, // affichage pour le fournisseur connecté
                    Montant = r.PrixTotal,
                    DateFacture = r.DateReservation,
                    Statut = "Payée",
                    NomClient = r.NomClient,
                    EmailClient = r.EmailClient,
                    NomFournisseur = User.Identity?.Name ?? "Fournisseur",
                    EmailFournisseur = User.Identity?.Name ?? ""
                }).ToList();
            }

            return View(factures);
        }

        // GET: Facturation/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);
            var isSupplier = user?.TypeUtilisateur == "Fournisseur/Organisateur";

            var facture = await _context.Factures
                .Include(i => i.Reservation)
                    .ThenInclude(r => r.Seance)
                        .ThenInclude(s => s.Film)
                .Include(i => i.Reservation)
                    .ThenInclude(r => r.Seance)
                        .ThenInclude(s => s.Salle)
                .Include(i => i.Client)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (facture == null)
            {
                return NotFound();
            }

            // Vérifier les autorisations : le client peut voir ses factures, le fournisseur peut voir ses factures
            if (!isSupplier && facture.ClientId != userId)
            {
                TempData["ErrorMessage"] = "Accès refusé. Vous ne pouvez consulter que vos propres factures.";
                return RedirectToAction("Index", "Home");
            }

            if (isSupplier && facture.FournisseurId != userId)
            {
                TempData["ErrorMessage"] = "Accès refusé. Vous ne pouvez consulter que vos propres factures.";
                return RedirectToAction("Index", "Facturation");
            }
            // Récupérer les sièges liés à la réservation pour affichage
            var sieges = await _context.Sieges
                .Where(s => s.ReservationId == facture.ReservationId)
                .OrderBy(s => s.Rang).ThenBy(s => s.Numero)
                .ToListAsync();
            ViewBag.Sieges = sieges;
            return View(facture);
        }

        // GET: Facturation/PDF/5
        public async Task<IActionResult> PDF(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var facture = await _context.Factures
                .Include(i => i.Reservation)
                    .ThenInclude(r => r.Seance)
                        .ThenInclude(s => s.Film)
                .Include(i => i.Client)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (facture == null)
            {
                return NotFound();
            }

            // Récupérer les sièges pour l'impression
            var sieges = await _context.Sieges
                .Where(s => s.ReservationId == facture.ReservationId)
                .OrderBy(s => s.Rang).ThenBy(s => s.Numero)
                .ToListAsync();

            // Générer le contenu HTML de la facture
            var htmlContent = GenerateInvoiceHtml(facture, sieges);
            
            // Pour l'instant, on retourne le HTML
            // Dans une vraie application, on utiliserait une librairie comme iTextSharp pour générer un PDF
            return Content(htmlContent, "text/html");
        }

        // GET: Facturation/Statistiques
        public async Task<IActionResult> Statistiques()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var factures = await _context.Factures
                .Where(i => i.FournisseurId == userId && i.Statut == "Payée")
                .ToListAsync();

            var statistiques = new
            {
                TotalRevenus = factures.Sum(f => f.Montant),
                NombreFactures = factures.Count,
                RevenusMoyens = factures.Any() ? factures.Average(f => f.Montant) : 0,
                RevenusCeMois = factures.Where(f => f.DateFacture.Month == DateTime.Now.Month && f.DateFacture.Year == DateTime.Now.Year).Sum(f => f.Montant),
                RevenusCeMoisDernier = factures.Where(f => f.DateFacture.Month == DateTime.Now.AddMonths(-1).Month && f.DateFacture.Year == DateTime.Now.AddMonths(-1).Year).Sum(f => f.Montant),
                FacturesRecent = factures.OrderByDescending(f => f.DateFacture).Take(10).ToList()
            };

            return View(statistiques);
        }

        private string GenerateInvoiceHtml(Facture facture, List<Siege> sieges)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Facture {facture.NumeroFacture}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .company-info {{ margin-bottom: 30px; }}
        .invoice-details {{ margin-bottom: 30px; }}
        .items {{ width: 100%; border-collapse: collapse; margin-bottom: 30px; }}
        .items th, .items td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        .items th {{ background-color: #f2f2f2; }}
        .total {{ text-align: right; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>FACTURE</h1>
        <h2>Numéro: {facture.NumeroFacture}</h2>
    </div>
    
    <div class='company-info'>
        <h3>Fournisseur:</h3>
        <p>{facture.NomFournisseur}<br>
        {facture.EmailFournisseur}<br>
        {facture.AdresseFournisseur}<br>
        {facture.CodePostalFournisseur} {facture.VilleFournisseur}</p>
    </div>
    
    <div class='invoice-details'>
        <h3>Client:</h3>
        <p>{facture.NomClient}<br>
        {facture.EmailClient}<br>
        {facture.AdresseClient}<br>
        {facture.CodePostalClient} {facture.VilleClient}</p>
        
        <p><strong>Date de facture:</strong> {facture.DateFacture:dd/MM/yyyy}</p>
        <p><strong>Statut:</strong> {facture.Statut}</p>
    </div>
    
    <table class='items'>
        <thead>
            <tr>
                <th>Film</th>
                <th>Date/Heure</th>
                <th>Places</th>
                <th>Prix unitaire</th>
                <th>Total</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td>{facture.Reservation.Seance.Film.Titre}</td>
                <td>{facture.Reservation.Seance.DateHeure:dd/MM/yyyy HH:mm}</td>
                <td>{facture.Reservation.NombrePlaces}</td>
                <td>{facture.Reservation.PrixUnitaire:C}</td>
                <td>{facture.Montant:C}</td>
            </tr>
            <tr>
                <td colspan='5'>
                    <strong>Sièges:</strong> {(sieges != null && sieges.Any() ? string.Join(", ", sieges.Select(s => s.NomComplet)) : "Non renseignés")}
                </td>
            </tr>
        </tbody>
    </table>
    
    <div class='total'>
        <h3>Total: {facture.Montant:C}</h3>
    </div>
</body>
</html>";
        }
    }
}
