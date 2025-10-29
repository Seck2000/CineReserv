using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CineReserv.Data;
using CineReserv.Models;
using Stripe;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace CineReserv.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PaymentController> _logger;
        private readonly StripeSettings _stripeSettings;

        public PaymentController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<PaymentController> logger,
            IOptions<StripeSettings> stripeSettings)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _stripeSettings = stripeSettings.Value;
        }

        // Action pour récupérer la clé publique Stripe
        [HttpGet]
        public IActionResult GetPublishableKey()
        {
            return Json(new { publishableKey = _stripeSettings.PublishableKey });
        }

        // Action pour traiter le paiement avec token Stripe
        [HttpPost]
        public async Task<IActionResult> Charge(string stripeToken)
        {
            try
            {
                if (!User.Identity?.IsAuthenticated == true)
                {
                    TempData["ErrorMessage"] = "Utilisateur non authentifié";
                    return RedirectToAction("Index", "Panier");
                }

                if (string.IsNullOrEmpty(stripeToken))
                {
                    TempData["ErrorMessage"] = "Token de paiement manquant";
                    return RedirectToAction("Index");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Récupérer les éléments du panier
                var panierItems = await _context.PanierItems
                    .Include(p => p.Seance)
                        .ThenInclude(s => s.Film)
                    .Include(p => p.Seance)
                        .ThenInclude(s => s.Salle)
                    .Include(p => p.CategorieAge)
                    .Where(p => p.UserId == userId)
                    .ToListAsync();

                if (!panierItems.Any())
                {
                    TempData["ErrorMessage"] = "Panier vide";
                    return RedirectToAction("Index", "Panier");
                }

                // Calculer le montant total
                decimal totalAmount = panierItems.Sum(item => item.PrixTotal);
                long amountInCents = (long)(totalAmount * 100);

                // Récupérer les informations utilisateur
                var user = await _userManager.GetUserAsync(User);

                // Création d'un objet avec les informations de paiement
                var chargeOptions = new ChargeCreateOptions
                {
                    Amount = amountInCents,
                    Currency = "eur",
                    Description = $"Réservation CineReserv - {user?.Prenom} {user?.Nom}",
                    Source = stripeToken,
                    ReceiptEmail = user?.Email,
                    // Métadonnées liées au client
                    Metadata = new Dictionary<string, string>
                    {
                        { "customer_name", $"{user?.Prenom} {user?.Nom}" },
                        { "customer_email", user?.Email ?? "" },
                        { "userId", userId },
                        { "panierItems", string.Join(",", panierItems.Select(i => i.Id)) }
                    }
                };

                var chargeService = new ChargeService();
                Charge charge = chargeService.Create(chargeOptions);

                if (charge.Status == "succeeded")
                {
                    // Créer les réservations
                    var reservations = new List<Reservation>();
                    foreach (var item in panierItems)
                    {
                        var reservation = new Reservation
                        {
                            UserId = userId,
                            SeanceId = item.SeanceId,
                            CategorieAgeId = item.CategorieAgeId,
                            NomClient = user?.Prenom + " " + user?.Nom,
                            EmailClient = user?.Email ?? "",
                            TelephoneClient = string.Empty,
                            NombrePlaces = item.Quantite,
                            PrixUnitaire = item.PrixTotal / item.Quantite,
                            PrixTotal = item.PrixTotal,
                            DateReservation = DateTime.Now,
                            Statut = "Confirmée",
                            NumeroReservation = $"RES-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                            StripeChargeId = charge.Id
                        };
                        reservations.Add(reservation);
                    }

                    // Sauvegarder les réservations
                    _context.Reservations.AddRange(reservations);
                    await _context.SaveChangesAsync();

                    // Créer les sièges réservés
                    var sieges = new List<Siege>();
                    foreach (var reservation in reservations)
                    {
                        var seance = await _context.Seances
                            .Include(s => s.Salle)
                            .FirstAsync(s => s.Id == reservation.SeanceId);

                        for (int i = 0; i < reservation.NombrePlaces; i++)
                        {
                            var siege = new Siege
                            {
                                SalleId = seance.SalleId,
                                UserId = userId,
                                Rang = "A",
                                Numero = i + 1,
                                Type = "Standard",
                                EstReserve = true,
                                DateReservation = DateTime.Now
                            };
                            sieges.Add(siege);
                        }
                    }

                    // Sauvegarder les sièges
                    _context.Sieges.AddRange(sieges);

                    // Créer les factures (une par réservation)
                    var invoices = new List<CineReserv.Models.Invoice>();
                    foreach (var reservation in reservations)
                    {
                        var seance = await _context.Seances
                            .Include(s => s.Film)
                            .FirstAsync(s => s.Id == reservation.SeanceId);

                        var invoice = new CineReserv.Models.Invoice
                        {
                            NumeroFacture = $"INV-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                            ReservationId = reservation.Id,
                            ClientId = reservation.UserId,
                            FournisseurId = seance.FournisseurId ?? string.Empty,
                            Montant = reservation.PrixTotal,
                            DateFacture = DateTime.Now,
                            Statut = "Payée",
                            NomClient = reservation.NomClient,
                            EmailClient = reservation.EmailClient,
                            NomFournisseur = seance.FournisseurId ?? string.Empty,
                            EmailFournisseur = string.Empty
                        };
                        invoices.Add(invoice);
                    }
                    _context.Invoices.AddRange(invoices);

                    // Vider le panier
                    _context.PanierItems.RemoveRange(panierItems);

                    await _context.SaveChangesAsync();

                    // Redirection vers l'action Success (Success.cshtml)
                    return RedirectToAction("Success");
                }
                else
                {
                    // Redirection vers l'action Failure (Failure.cshtml)
                    return RedirectToAction("Failure");
                }
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Erreur Stripe lors du traitement du paiement");
                // En cas d'erreur Stripe (carte refusée, etc.), rediriger vers Failure
                TempData["ErrorMessage"] = $"Erreur de paiement : {ex.Message}";
                return RedirectToAction("Failure");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement du paiement");
                // Pour toute autre erreur, rediriger vers Failure
                TempData["ErrorMessage"] = "Une erreur est survenue lors du traitement du paiement";
                return RedirectToAction("Failure");
            }
        }

        // Action pour afficher la page de paiement
        public async Task<IActionResult> Index()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var panierItems = await _context.PanierItems
                .Include(p => p.Seance)
                    .ThenInclude(s => s.Film)
                .Include(p => p.Seance)
                    .ThenInclude(s => s.Salle)
                .Include(p => p.CategorieAge)
                .Where(p => p.UserId == userId)
                .ToListAsync();

            if (!panierItems.Any())
            {
                TempData["ErrorMessage"] = "Votre panier est vide";
                return RedirectToAction("Index", "Panier");
            }

            ViewBag.PanierItems = panierItems;
            ViewBag.TotalAmount = panierItems.Sum(item => item.PrixTotal);

            return View();
        }

        // Action pour afficher la page de succès
        public IActionResult Success()
        {
            return View();
        }

        // Action pour afficher la page d'échec
        public IActionResult Failure()
        {
            return View();
        }

        // Action pour gérer les webhooks Stripe
        [HttpPost]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    "whsec_your_webhook_secret" // À remplacer par votre webhook secret
                );

                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    _logger.LogInformation($"Paiement réussi: {paymentIntent?.Id}");
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement du webhook Stripe");
                return BadRequest();
            }
        }
    }

    // Classes pour les requêtes
    public class PaymentRequest
    {
        public List<int> PanierItemIds { get; set; } = new List<int>();
    }

    public class PaymentConfirmationRequest
    {
        public string PaymentIntentId { get; set; } = string.Empty;
    }
}
