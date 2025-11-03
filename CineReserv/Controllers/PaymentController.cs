using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CineReserv.Data;
using CineReserv.Models;
using CineReserv.Helpers;
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
                    // Redirection silencieuse sans message
                    return RedirectToAction("Index", "Panier");
                }

                if (string.IsNullOrEmpty(stripeToken))
                {
                    // Redirection silencieuse sans message
                    return RedirectToAction("Index", "Panier");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Récupérer les éléments du panier avec les sièges
                var panierItems = await _context.PanierItems
                    .Include(p => p.Seance)
                        .ThenInclude(s => s.Film)
                    .Include(p => p.Seance)
                        .ThenInclude(s => s.Salle)
                            .ThenInclude(s => s.Sieges)
                    .Include(p => p.CategorieAge)
                    .Where(p => p.UserId == userId)
                    .ToListAsync();

                if (!panierItems.Any())
                {
                    // Redirection silencieuse sans message
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
                    Currency = "usd",
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

                    // Utiliser les sièges réels sélectionnés depuis le panier
                    var siegeIndex = 0;
                    foreach (var item in panierItems)
                    {
                        // Trouver la réservation correspondante à cet item du panier
                        var reservation = reservations.FirstOrDefault(r => r.SeanceId == item.SeanceId && r.CategorieAgeId == item.CategorieAgeId);
                        if (reservation == null) continue;

                        // Récupérer les IDs des sièges sélectionnés depuis le panier
                        if (!string.IsNullOrEmpty(item.SiegeIds))
                        {
                            var siegeIds = SiegeHelper.ParseSiegeIds(item.SiegeIds);

                            // Récupérer les sièges réels depuis la base de données
                            var siegesSelectionnes = await SiegeHelper.GetSiegesByIdsAsync(_context, siegeIds);

                            // Vérifier qu'ils ne sont pas déjà réservés pour CETTE SÉANCE SPÉCIFIQUE par un autre utilisateur
                            // 1. Sièges confirmés (payés) pour cette séance par un autre utilisateur
                            var siegeIdsReservesConfirmesPourCetteSeance = await _context.Reservations
                                .Where(r => r.SeanceId == reservation.SeanceId && r.UserId != userId)
                                .SelectMany(r => _context.Sieges
                                    .Where(s => s.ReservationId == r.Id && siegeIds.Contains(s.Id))
                                    .Select(s => s.Id))
                                .ToListAsync();

                            // 2. Sièges dans le panier d'un autre utilisateur pour cette séance
                            var panierItemsAutresUtilisateurs = await _context.PanierItems
                                .Where(p => p.SeanceId == reservation.SeanceId && 
                                           p.UserId != userId && 
                                           !string.IsNullOrEmpty(p.SiegeIds))
                                .Select(p => p.SiegeIds)
                                .ToListAsync();
                            
                            var siegeIdsDansPanierAutres = panierItemsAutresUtilisateurs
                                .SelectMany(p => SiegeHelper.ParseSiegeIds(p))
                                .Where(id => siegeIds.Contains(id))
                                .Distinct()
                                .ToList();

                            // Combiner tous les sièges déjà réservés pour cette séance par d'autres utilisateurs
                            var siegeIdsDejaReservesPourCetteSeance = siegeIdsReservesConfirmesPourCetteSeance
                                .Union(siegeIdsDansPanierAutres)
                                .ToList();

                            // Vérifier si les sièges sélectionnés sont déjà réservés pour cette séance
                            if (siegeIdsDejaReservesPourCetteSeance.Any())
                            {
                                // Redirection silencieuse sans message - sièges déjà réservés pour cette séance
                                return RedirectToAction("Index", "Panier");
                            }

                            // Vérifier aussi les sièges physiquement occupés
                            var siegesOccupeOuReserveGlobalement = siegesSelectionnes.Where(s => 
                                s.EstOccupe || 
                                (s.EstReserve && s.UserId != userId && s.ReservationId != null)).ToList();
                            
                            if (siegesOccupeOuReserveGlobalement.Any())
                            {
                                // Redirection silencieuse sans message - sièges occupés ou réservés
                                return RedirectToAction("Index", "Panier");
                            }

                            // Lier les sièges à la réservation et confirmer la réservation
                            foreach (var siege in siegesSelectionnes)
                            {
                                siege.ReservationId = reservation.Id; // Lier à la réservation confirmée
                                siege.UserId = userId;
                                siege.EstReserve = true; // Marquer comme réservé définitivement
                                siege.DateReservation = DateTime.Now;
                            }

                            await _context.SaveChangesAsync();
                        }
                        // NOTE: Suppression du fallback qui créait des doublons de sièges (A1, A2, etc.)
                        // Si aucun siège n'a été sélectionné (SiegeIds vide), c'est une erreur qui ne devrait jamais arriver.
                        // Si cela arrive, on ignore simplement cette réservation plutôt que de créer des doublons.
                    }

                    // Créer les factures (une par réservation)
                    var factures = new List<CineReserv.Models.Facture>();
                    foreach (var reservation in reservations)
                    {
                        var seance = await _context.Seances
                            .Include(s => s.Film)
                            .FirstAsync(s => s.Id == reservation.SeanceId);

                        var facture = new CineReserv.Models.Facture
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
                        factures.Add(facture);
                    }
                    _context.Factures.AddRange(factures);

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
                // Redirection silencieuse vers Failure sans message
                return RedirectToAction("Failure");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement du paiement");
                // Redirection silencieuse vers Failure sans message
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
                // Redirection silencieuse sans message
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
