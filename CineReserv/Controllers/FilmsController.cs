using CineReserv.Data;
using CineReserv.Models;
using CineReserv.Services;
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return false;
            
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            return user?.TypeUtilisateur == "Fournisseur/Organisateur";
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

            TempData["SuccessMessage"] = $"Réservation confirmée ! Numéro: {reservation.NumeroReservation}";
            return RedirectToAction(nameof(Details), new { id = seance.FilmId });
        }

        // GET: Films/SelectionBillets
        public async Task<IActionResult> SelectionBillets(int seanceId)
        {
            // Empêcher les fournisseurs de réserver
            if (IsSupplier())
            {
                TempData["ErrorMessage"] = "Les fournisseurs ne peuvent pas réserver de séances. Utilisez le tableau de bord pour gérer vos offres.";
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
        public async Task<IActionResult> ConfirmerReservation(int seanceId, Dictionary<int, int> quantities)
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

                // Vérifier qu'au moins une quantité est sélectionnée
                var totalQuantity = quantities.Values.Sum();
                if (totalQuantity == 0)
                {
                    TempData["ErrorMessage"] = "Veuillez sélectionner au moins un billet.";
                    return RedirectToAction("SelectionBillets", new { seanceId });
                }

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
                TempData["ErrorMessage"] = "Une erreur est survenue lors de la préparation de la réservation.";
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
                TempData["ErrorMessage"] = "Aucune donnée de réservation trouvée.";
                return RedirectToAction("Index", "Films");
            }

            var reservationData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(reservationDataJson);
            var totalQuantity = (int)reservationData.GetProperty("TotalQuantity").GetInt32();

            // Récupérer les sièges de la salle
            var sieges = await _context.Sieges
                .Where(s => s.SalleId == seance.SalleId)
                .OrderBy(s => s.Rang)
                .ThenBy(s => s.Numero)
                .ToListAsync();

            // Si pas de sièges, en créer automatiquement
            if (!sieges.Any())
            {
                await CreateSiegesForSalle(seance.SalleId, seance.Salle.NombrePlaces);
                sieges = await _context.Sieges
                    .Where(s => s.SalleId == seance.SalleId)
                    .OrderBy(s => s.Rang)
                    .ThenBy(s => s.Numero)
                    .ToListAsync();
            }

            // Pré-attribuer des sièges automatiquement
            var siegesDisponibles = sieges.Where(s => !s.EstOccupe && !s.EstReserve).ToList();
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

            return View();
        }

        // Méthode pour créer automatiquement les sièges
        private async Task CreateSiegesForSalle(int salleId, int nombrePlaces)
        {
            var sieges = new List<Siege>();
            var rangs = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T" };
            var siegesParRang = nombrePlaces / rangs.Length;
            var reste = nombrePlaces % rangs.Length;

            for (int i = 0; i < rangs.Length && sieges.Count < nombrePlaces; i++)
            {
                var rang = rangs[i];
                var nombreSiegesDansRang = siegesParRang + (i < reste ? 1 : 0);

                for (int j = 1; j <= nombreSiegesDansRang; j++)
                {
                    // Ajouter quelques sièges d'handicapé
                    var typeSiege = "Standard";
                    if (i >= 2 && i <= 4 && (j == 1 || j == nombreSiegesDansRang)) // Rangs C, D, E aux extrémités
                    {
                        typeSiege = "Handicape";
                    }

                    sieges.Add(new Siege
                    {
                        SalleId = salleId,
                        Rang = rang,
                        Numero = j,
                        Type = typeSiege,
                        EstOccupe = false,
                        EstReserve = false
                    });
                }
            }

            _context.Sieges.AddRange(sieges);
            await _context.SaveChangesAsync();
        }

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
                // Convertir la chaîne de sièges en liste d'entiers
                var siegeIdsList = new List<int>();
                if (!string.IsNullOrEmpty(siegeIds))
                {
                    siegeIdsList = siegeIds.Split(',')
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Select(int.Parse)
                        .ToList();
                }

                // Récupérer les données de réservation depuis la session
                var reservationDataJson = HttpContext.Session.GetString("ReservationData");
                if (string.IsNullOrEmpty(reservationDataJson))
                {
                    TempData["ErrorMessage"] = "Aucune donnée de réservation trouvée.";
                    return RedirectToAction("Index", "Films");
                }

                var reservationData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(reservationDataJson);
                var quantities = reservationData.GetProperty("Quantities");
                var totalQuantity = (int)reservationData.GetProperty("TotalQuantity").GetInt32();

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

                foreach (var kvp in quantities.EnumerateObject())
                {
                    var categorieId = int.Parse(kvp.Name);
                    var quantite = kvp.Value.GetInt32();
                    
                    if (quantite > 0)
                    {
                        var categorie = categoriesAge.FirstOrDefault(c => c.Id == categorieId);
                        if (categorie != null)
                        {
                            var panierItem = new PanierItem
                            {
                                SeanceId = seanceId,
                                CategorieAgeId = categorieId,
                                Quantite = quantite,
                                PrixUnitaire = categorie.Prix,
                                SessionId = sessionId,
                                UserId = userId,
                                DateAjout = DateTime.Now
                            };

                            _context.PanierItems.Add(panierItem);
                        }
                    }
                }

                // Marquer les sièges comme réservés
                var siegesAReserver = await _context.Sieges
                    .Where(s => siegeIdsList.Contains(s.Id))
                    .ToListAsync();

                foreach (var siege in siegesAReserver)
                {
                    siege.EstReserve = true;
                    siege.UserId = userId;
                    siege.DateReservation = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                // Nettoyer la session
                HttpContext.Session.Remove("ReservationData");

                TempData["SuccessMessage"] = "Réservation ajoutée au panier avec succès !";
                TempData["ShowSuccessAlert"] = "true";
                return RedirectToAction("Index", "Panier");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout au panier");
                TempData["ErrorMessage"] = "Une erreur est survenue lors de l'ajout au panier.";
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
                TempData["SuccessMessage"] = "Base de données peuplée avec succès !";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du peuplement de la base de données");
                TempData["ErrorMessage"] = "Erreur lors du peuplement de la base de données";
            }

            return RedirectToAction(nameof(Index));
        }

        private string GenerateReservationNumber()
        {
            return $"RES{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }
    }
}
