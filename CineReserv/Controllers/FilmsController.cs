using CineReserv.Data;
using CineReserv.Models;
using CineReserv.Services;
using CineReserv.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CineReserv.Controllers
{
    [Authorize]
    public class FilmsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IApiService _apiService;
        private readonly ILogger<FilmsController> _logger;

        public FilmsController(ApplicationDbContext context, IApiService apiService, ILogger<FilmsController> logger)
        {
            _context = context;
            _apiService = apiService;
            _logger = logger;
        }

        private bool IsSupplier()
        {
            return UserHelper.IsSupplier(this, _context);
        }

        // GET: Films
        public async Task<IActionResult> Index(string? genre, string? recherche, DateTime? dateDebut, DateTime? dateFin, decimal? prixMin, decimal? prixMax)
        {
            // Films à l'affiche = films qui ont des séances futures
            var query = _context.Films
                .Where(f => f.EstActif)
                .Where(f => f.Seances.Any(s => s.DateHeure > DateTime.Now && s.EstActive))
                .AsQueryable();

            // Filtre par genre
            if (!string.IsNullOrEmpty(genre))
            {
                query = query.Where(f => f.Genre == genre);
            }

            // Recherche par titre ou description
            if (!string.IsNullOrEmpty(recherche))
            {
                query = query.Where(f => f.Titre.Contains(recherche) || f.Description.Contains(recherche));
            }

            // Filtre par date de sortie
            if (dateDebut.HasValue)
            {
                query = query.Where(f => f.DateSortie >= dateDebut.Value);
            }
            if (dateFin.HasValue)
            {
                query = query.Where(f => f.DateSortie <= dateFin.Value);
            }

            // Filtre par prix
            if (prixMin.HasValue)
            {
                query = query.Where(f => f.Prix >= prixMin.Value);
            }
            if (prixMax.HasValue)
            {
                query = query.Where(f => f.Prix <= prixMax.Value);
            }

            var films = await query
                .Include(f => f.Seances)
                .OrderByDescending(f => f.DateSortie)
                .ToListAsync();

            // Récupérer les genres pour le filtre
            var genres = await _context.Films
                .Where(f => f.EstActif)
                .Select(f => f.Genre)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            ViewBag.Genres = genres;
            ViewBag.GenreSelectionne = genre;
            ViewBag.Recherche = recherche;
            ViewBag.DateDebut = dateDebut;
            ViewBag.DateFin = dateFin;
            ViewBag.PrixMin = prixMin;
            ViewBag.PrixMax = prixMax;
            ViewBag.IsSupplier = IsSupplier();

            // Nettoyer tous les messages d'erreur sur la page Films
            if (TempData["ErrorMessage"] != null)
            {
                TempData.Remove("ErrorMessage");
            }

            return View(films);
        }

        // GET: Films/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var film = await _context.Films
                .Include(f => f.Seances)
                    .ThenInclude(s => s.Salle)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (film == null)
            {
                return NotFound();
            }

            // Filtrer les séances futures
            film.Seances = film.Seances
                .Where(s => s.DateHeure > DateTime.Now && s.EstActive)
                .OrderBy(s => s.DateHeure)
                .ToList();

            // Vérifier si l'utilisateur est un fournisseur
            ViewBag.IsSupplier = IsSupplier();

            return View(film);
        }

        // GET: Films/Reserver/5
        public async Task<IActionResult> Reserver(int? id)
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

        // POST: Films/Reserver
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserver(int seanceId, int nombrePlaces, string nomClient, string emailClient, string telephoneClient)
        {
            var seance = await _context.Seances
                .Include(s => s.Film)
                .Include(s => s.Salle)
                .FirstOrDefaultAsync(s => s.Id == seanceId);

            if (seance == null)
            {
                return NotFound();
            }

            // Vérifier la disponibilité
            var reservationsExistantes = await _context.Reservations
                .Where(r => r.SeanceId == seanceId)
                .SumAsync(r => r.NombrePlaces);

            if (reservationsExistantes + nombrePlaces > seance.Salle.NombrePlaces)
            {
                ModelState.AddModelError("", "Pas assez de places disponibles");
                return View(seance);
            }

            // Créer la réservation
            var reservation = new Reservation
            {
                SeanceId = seanceId,
                UserId = User.Identity?.IsAuthenticated == true ? User.Identity.Name : "Anonymous",
                NomClient = nomClient,
                EmailClient = emailClient,
                TelephoneClient = telephoneClient,
                NombrePlaces = nombrePlaces,
                PrixTotal = seance.Prix * nombrePlaces,
                DateReservation = DateTime.Now,
                Statut = "Confirmee",
                NumeroReservation = GenerateReservationNumber()
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Redirection silencieuse sans message
            return RedirectToAction(nameof(Details), new { id = seance.FilmId });
        }

        // GET: Films/SelectionBillets
        public async Task<IActionResult> SelectionBillets(int seanceId)
        {
            // Empêcher les fournisseurs de réserver - redirection silencieuse
            if (IsSupplier())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var seance = await _context.Seances
                .Include(s => s.Film)
                .Include(s => s.Salle)
                .FirstOrDefaultAsync(s => s.Id == seanceId);

            if (seance == null)
            {
                return NotFound();
            }

            var categoriesAge = await _context.CategoriesAge
                .Where(c => c.EstActive)
                .OrderBy(c => c.Prix)
                .ToListAsync();

            ViewBag.Seance = seance;
            ViewBag.CategoriesAge = categoriesAge;

            return View();
        }

        // POST: Films/ConfirmerReservation
        [HttpPost]
        public async Task<IActionResult> ConfirmerReservation(int seanceId, Dictionary<int, int>? quantities)
        {
            try
            {
                var seance = await _context.Seances
                    .Include(s => s.Film)
                    .Include(s => s.Salle)
                    .FirstOrDefaultAsync(s => s.Id == seanceId);

                if (seance == null)
                {
                    return NotFound();
                }

                // Vérifier que quantities n'est pas null et qu'au moins une quantité est sélectionnée
                if (quantities == null || quantities.Count == 0 || quantities.Values.Sum() == 0)
                {
                    // Redirection silencieuse sans message
                    return RedirectToAction("SelectionBillets", new { seanceId });
                }

                var totalQuantity = quantities.Values.Sum();

                // Stocker les quantités en session pour la sélection des sièges
                var sessionId = HttpContext.Session.GetString("SessionId") ?? Guid.NewGuid().ToString();
                HttpContext.Session.SetString("SessionId", sessionId);
                
                // Stocker les données de réservation en session
                var reservationData = new
                {
                    SeanceId = seanceId,
                    Quantities = quantities,
                    TotalQuantity = totalQuantity
                };
                
                HttpContext.Session.SetString("ReservationData", System.Text.Json.JsonSerializer.Serialize(reservationData));

                // Rediriger vers la sélection des sièges SANS ajouter au panier
                return RedirectToAction("SelectionSieges", new { seanceId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la préparation de la réservation");
                // Redirection silencieuse sans message
                return RedirectToAction("SelectionBillets", new { seanceId });
            }
        }

        // GET: Films/SelectionSieges
        public async Task<IActionResult> SelectionSieges(int seanceId)
        {
            var seance = await _context.Seances
                .Include(s => s.Film)
                .Include(s => s.Salle)
                .FirstOrDefaultAsync(s => s.Id == seanceId);

            if (seance == null)
            {
                return NotFound();
            }

            // Récupérer les données de réservation depuis la session
            var reservationDataJson = HttpContext.Session.GetString("ReservationData");
            if (string.IsNullOrEmpty(reservationDataJson))
            {
                // Rediriger silencieusement vers la sélection des billets si la session est perdue
                return RedirectToAction("SelectionBillets", new { seanceId });
            }

            // Désérialiser les données de réservation
            int totalQuantity = 1; // Valeur par défaut
            try
            {
                var reservationDataElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(reservationDataJson);
                if (reservationDataElement.TryGetProperty("TotalQuantity", out var totalQuantityProp))
                {
                    totalQuantity = totalQuantityProp.GetInt32();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erreur lors de la désérialisation des données de réservation, utilisation de la valeur par défaut");
                // Utiliser la valeur par défaut de 1 si la désérialisation échoue
            }

            // Récupérer l'utilisateur actuel pour vérifier les sièges qu'il a déjà réservés
            var userId = User.Identity?.IsAuthenticated == true 
                ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                : null;

            // Récupérer les sièges de la salle avec leurs réservations
            // Les sièges sont fixes à 200 par salle et doivent déjà exister (créés lors du peuplement initial)
            var sieges = await _context.Sieges
                .Include(s => s.Reservation)
                .Where(s => s.SalleId == seance.SalleId)
                .OrderBy(s => s.Rang)
                .ThenBy(s => s.Numero)
                .ToListAsync();

            // Si aucun siège n'existe, c'est une erreur - les sièges doivent être créés lors du peuplement initial
            if (!sieges.Any())
            {
                _logger.LogWarning($"Aucun siège trouvé pour la salle {seance.SalleId}. Les sièges doivent être créés lors du peuplement initial.");
                // Rediriger silencieusement sans message
                return RedirectToAction("SelectionBillets", new { seanceId });
            }

            // Récupérer les IDs des sièges réservés pour cette séance spécifique
            // 1. Sièges réservés confirmés (payés) pour cette séance
            var siegeIdsReservesConfirmes = await _context.Reservations
                .Where(r => r.SeanceId == seanceId)
                .SelectMany(r => _context.Sieges
                    .Where(s => s.ReservationId == r.Id)
                    .Select(s => s.Id))
                .ToListAsync();

            // 2. Sièges dans le panier pour cette séance spécifique
            var panierItemsPourCetteSeance = await _context.PanierItems
                .Where(p => p.SeanceId == seanceId && !string.IsNullOrEmpty(p.SiegeIds))
                .Select(p => p.SiegeIds)
                .ToListAsync();
            
            var siegeIdsDansPanier = panierItemsPourCetteSeance
                .SelectMany(p => SiegeHelper.ParseSiegeIds(p))
                .Distinct()
                .ToList();

            // Combiner tous les sièges réservés pour cette séance
            var siegeIdsReservesPourCetteSeance = siegeIdsReservesConfirmes.Union(siegeIdsDansPanier).ToList();

            // Pré-attribuer des sièges automatiquement (seulement ceux qui ne sont ni occupés ni réservés pour cette séance)
            var siegesDisponibles = sieges.Where(s => 
                !s.EstOccupe && 
                !siegeIdsReservesPourCetteSeance.Contains(s.Id)).ToList();
            var siegesPreAttribues = new List<Siege>();

            if (totalQuantity == 1)
            {
                // 1 billet = 1 siège libre
                var siegeLibre = siegesDisponibles.FirstOrDefault();
                if (siegeLibre != null)
                {
                    siegesPreAttribues.Add(siegeLibre);
                }
            }
            else if (totalQuantity == 2)
            {
                // 2 billets = 2 sièges côte à côte
                var siegesAdjacents = FindAdjacentSeats(siegesDisponibles, 2);
                if (siegesAdjacents.Any())
                {
                    siegesPreAttribues.AddRange(siegesAdjacents);
                }
            }
            else
            {
                // Plus de 2 billets = sièges adjacents
                var siegesAdjacents = FindAdjacentSeats(siegesDisponibles, totalQuantity);
                if (siegesAdjacents.Any())
                {
                    siegesPreAttribues.AddRange(siegesAdjacents);
                }
            }

            ViewBag.Seance = seance;
            ViewBag.Sieges = sieges;
            ViewBag.SiegesPreAttribues = siegesPreAttribues.Select(s => s.Id).ToList();
            ViewBag.TotalQuantity = totalQuantity;
            ViewBag.ReservationData = reservationDataJson;
            ViewBag.CurrentUserId = userId; // Pour vérifier les sièges réservés par l'utilisateur actuel
            ViewBag.SiegeIdsReservesPourCetteSeance = siegeIdsReservesPourCetteSeance; // IDs des sièges réservés pour cette séance spécifique
            ViewBag.CurrentSeanceId = seanceId; // ID de la séance en cours

            return View();
        }

        // NOTE: Les sièges sont fixes à 200 par salle et sont créés lors du peuplement initial (SeedDatabaseAsync)
        // Cette méthode ne devrait jamais être appelée - elle est conservée uniquement pour référence
        // La création des sièges se fait uniquement dans ApiService.SeedDatabaseAsync() ou ApiService.UpdateAllSallesTo200PlacesAsync()

        // Méthode pour trouver des sièges adjacents
        private List<Siege> FindAdjacentSeats(List<Siege> siegesDisponibles, int nombreSieges)
        {
            var siegesAdjacents = new List<Siege>();
            
            // Grouper par rang
            var siegesParRang = siegesDisponibles.GroupBy(s => s.Rang).OrderBy(g => g.Key);
            
            foreach (var rang in siegesParRang)
            {
                var siegesDuRang = rang.OrderBy(s => s.Numero).ToList();
                
                for (int i = 0; i <= siegesDuRang.Count - nombreSieges; i++)
                {
                    var sequence = siegesDuRang.Skip(i).Take(nombreSieges).ToList();
                    
                    // Vérifier si la séquence est continue
                    bool isConsecutive = true;
                    for (int j = 1; j < sequence.Count; j++)
                    {
                        if (sequence[j].Numero != sequence[j-1].Numero + 1)
                        {
                            isConsecutive = false;
                            break;
                        }
                    }
                    
                    if (isConsecutive)
                    {
                        return sequence;
                    }
                }
            }
            
            return siegesAdjacents;
        }

        // POST: Films/ConfirmerSieges
        [HttpPost]
        public async Task<IActionResult> ConfirmerSieges(int seanceId, string siegeIds)
        {
            try
            {
                // Récupérer la séance pour vérifier qu'elle existe
                var seance = await _context.Seances
                    .Include(s => s.Salle)
                    .FirstOrDefaultAsync(s => s.Id == seanceId);

                if (seance == null)
                {
                    // Redirection silencieuse sans message
                    return RedirectToAction("Index", "Films");
                }

                // Convertir la chaîne de sièges en liste d'entiers
                var siegeIdsList = SiegeHelper.ParseSiegeIds(siegeIds);

                // Récupérer les données de réservation depuis la session
                var reservationDataJson = HttpContext.Session.GetString("ReservationData");
                if (string.IsNullOrEmpty(reservationDataJson))
                {
                    // Rediriger silencieusement vers la sélection des billets si la session est perdue
                    return RedirectToAction("SelectionBillets", new { seanceId });
                }

                var reservationData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(reservationDataJson);
                var quantitiesObj = reservationData.GetProperty("Quantities");
                var totalQuantity = (int)reservationData.GetProperty("TotalQuantity").GetInt32();
                
                // Convertir les quantités depuis le JSON dynamique
                Dictionary<int, int> quantities = new Dictionary<int, int>();
                foreach (var kvp in quantitiesObj.EnumerateObject())
                {
                    var categorieId = int.Parse(kvp.Name);
                    var quantite = kvp.Value.GetInt32();
                    quantities[categorieId] = quantite;
                }

                // Vérifier que le nombre de sièges correspond au nombre de billets
                if (siegeIdsList.Count != totalQuantity)
                {
                    TempData["ErrorMessage"] = $"Vous devez sélectionner exactement {totalQuantity} siège{(totalQuantity > 1 ? "s" : "")}.";
                    return RedirectToAction("SelectionSieges", new { seanceId });
                }

                // Vérifier que les sièges sont adjacents pour plus d'un billet
                if (totalQuantity > 1)
                {
                    var sieges = await _context.Sieges
                        .Where(s => siegeIdsList.Contains(s.Id))
                        .OrderBy(s => s.Rang)
                        .ThenBy(s => s.Numero)
                        .ToListAsync();

                    if (!AreSeatsAdjacent(sieges))
                    {
                        TempData["ErrorMessage"] = "Les sièges doivent être côte à côte.";
                        return RedirectToAction("SelectionSieges", new { seanceId });
                    }
                }

                // Récupérer les catégories d'âge
                var categoriesAge = await _context.CategoriesAge
                    .Where(c => c.EstActive)
                    .ToListAsync();

                // Créer les articles du panier
                var sessionId = HttpContext.Session.GetString("SessionId") ?? Guid.NewGuid().ToString();
                var userId = User.Identity?.IsAuthenticated == true ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;

                // Distribuer les sièges sélectionnés entre les différentes catégories
                var siegeIndex = 0;
                foreach (var kvp in quantities)
                {
                    var categorieId = kvp.Key;
                    var quantite = kvp.Value;
                    
                    if (quantite > 0)
                    {
                        var categorie = categoriesAge.FirstOrDefault(c => c.Id == categorieId);
                        if (categorie != null)
                        {
                            // Prendre les sièges correspondant à la quantité pour cette catégorie
                            var siegesPourCetteCategorie = new List<string>();
                            for (int i = 0; i < quantite && siegeIndex + i < siegeIdsList.Count; i++)
                            {
                                siegesPourCetteCategorie.Add(siegeIdsList[siegeIndex + i].ToString());
                            }
                            
                            siegeIndex += quantite;

                            // Calculer NombrePlaces basé sur Quantite
                            var panierItem = new PanierItem
                            {
                                SeanceId = seanceId,
                                CategorieAgeId = categorieId,
                                Quantite = quantite,
                                NombrePlaces = quantite,
                                PrixUnitaire = categorie.Prix,
                                SessionId = sessionId,
                                UserId = userId,
                                SiegeIds = string.Join(",", siegesPourCetteCategorie), // Stocker les IDs des sièges sélectionnés pour cette catégorie
                                DateAjout = DateTime.Now
                            };

                            _context.PanierItems.Add(panierItem);
                        }
                    }
                }

                // Vérifier que les sièges existent et sont disponibles
                var siegesAReserver = await _context.Sieges
                    .Where(s => siegeIdsList.Contains(s.Id) && s.SalleId == seance.SalleId)
                    .ToListAsync();

                // Vérifier que tous les sièges demandés existent
                if (siegesAReserver.Count != siegeIdsList.Count)
                {
                    var siegesManquants = siegeIdsList.Where(id => !siegesAReserver.Any(s => s.Id == id)).ToList();
                    TempData["ErrorMessage"] = $"Les sièges sélectionnés ne sont pas valides ou n'existent pas.";
                    return RedirectToAction("SelectionSieges", new { seanceId });
                }

                // Vérifier qu'il n'y a pas de doublons dans la sélection
                if (siegeIdsList.Count != siegeIdsList.Distinct().Count())
                {
                    TempData["ErrorMessage"] = "Vous ne pouvez pas sélectionner le même siège plusieurs fois.";
                    return RedirectToAction("SelectionSieges", new { seanceId });
                }

                // Vérifier que les sièges ne sont pas déjà réservés pour CETTE SÉANCE SPÉCIFIQUE
                // 1. Sièges physiquement occupés
                // 2. Sièges réservés confirmés (payés) pour cette séance
                // 3. Sièges dans le panier pour cette séance (réservés temporairement)
                
                // Sièges confirmés pour cette séance
                var siegeIdsReservesConfirmes = await _context.Reservations
                    .Where(r => r.SeanceId == seanceId)
                    .SelectMany(r => _context.Sieges
                        .Where(s => s.ReservationId == r.Id)
                        .Select(s => s.Id))
                    .ToListAsync();

                // Sièges dans le panier pour cette séance
                var panierItemsPourCetteSeanceCheck = await _context.PanierItems
                    .Where(p => p.SeanceId == seanceId && !string.IsNullOrEmpty(p.SiegeIds))
                    .Select(p => p.SiegeIds)
                    .ToListAsync();
                
                var siegeIdsDansPanier = panierItemsPourCetteSeanceCheck
                    .SelectMany(p => SiegeHelper.ParseSiegeIds(p))
                    .Distinct()
                    .ToList();

                // Combiner tous les sièges réservés pour cette séance
                var siegeIdsReservesPourCetteSeance = siegeIdsReservesConfirmes.Union(siegeIdsDansPanier).ToList();

                // Vérifier si les sièges sélectionnés sont déjà réservés pour cette séance
                var siegesDejaReserves = siegesAReserver.Where(s => 
                    s.EstOccupe || 
                    siegeIdsReservesPourCetteSeance.Contains(s.Id)).ToList();
                
                if (siegesDejaReserves.Any())
                {
                    // Redirection silencieuse sans message - les sièges sont déjà réservés
                    return RedirectToAction("SelectionSieges", new { seanceId });
                }

                // Marquer les sièges comme réservés temporairement (seront confirmés au paiement)
                foreach (var siege in siegesAReserver)
                {
                    siege.EstReserve = true;
                    siege.UserId = userId;
                    siege.DateReservation = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                // Nettoyer la session
                HttpContext.Session.Remove("ReservationData");

                // Redirection silencieuse sans message
                return RedirectToAction("Index", "Panier");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout au panier");
                // Redirection silencieuse sans message
                return RedirectToAction("SelectionSieges", new { seanceId });
            }
        }

        // Méthode pour vérifier si les sièges sont adjacents
        private bool AreSeatsAdjacent(List<Siege> sieges)
        {
            if (sieges.Count <= 1) return true;

            // Grouper par rang
            var siegesParRang = sieges.GroupBy(s => s.Rang);
            
            foreach (var rang in siegesParRang)
            {
                var siegesDuRang = rang.OrderBy(s => s.Numero).ToList();
                
                // Vérifier si tous les sièges sont consécutifs
                for (int i = 1; i < siegesDuRang.Count; i++)
                {
                    if (siegesDuRang[i].Numero != siegesDuRang[i-1].Numero + 1)
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }

        // GET: Films/SeedData
        public async Task<IActionResult> SeedData()
        {
            try
            {
                await _apiService.SeedDatabaseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du peuplement de la base de données");
            }

            // Redirection silencieuse sans message
            return RedirectToAction(nameof(Index));
        }

        private string GenerateReservationNumber()
        {
            return $"RES{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }
    }
}
