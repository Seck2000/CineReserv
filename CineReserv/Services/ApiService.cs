using CineReserv.Models;
using CineReserv.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CineReserv.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApiService> _logger;

        public ApiService(HttpClient httpClient, ApplicationDbContext context, ILogger<ApiService> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _logger = logger;
        }

        public async Task<List<Film>> GetFilmsFromApiAsync()
        {
            try
            {
                // Utilisation de Dummy JSON pour récupérer des produits qui peuvent servir de films
                var response = await _httpClient.GetStringAsync("https://dummyjson.com/products");
                var jsonDocument = JsonDocument.Parse(response);
                var products = jsonDocument.RootElement.GetProperty("products");

                var films = new List<Film>();
                var random = new Random();

                foreach (var product in products.EnumerateArray().Take(20)) // Récupérer 20 films pour la page d'accueil
                {
                    var film = new Film
                    {
                        Titre = product.GetProperty("title").GetString() ?? "Film sans titre",
                        Description = product.GetProperty("description").GetString() ?? "",
                        Genre = GetRandomGenre(),
                        Duree = random.Next(90, 180), // Durée entre 90 et 180 minutes
                        ImageUrl = product.GetProperty("thumbnail").GetString() ?? "",
                        DateSortie = DateTime.Now.AddDays(-random.Next(1, 365)),
                        Classification = GetRandomClassification(),
                        Prix = (decimal)(random.NextDouble() * 15 + 5), // Prix entre 5 et 20 euros
                        EstActif = true
                    };

                    films.Add(film);
                }

                return films;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des films depuis l'API");
                return new List<Film>();
            }
        }

        public async Task<Film> GetFilmByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"https://dummyjson.com/products/{id}");
                var product = JsonDocument.Parse(response).RootElement;
                var random = new Random();

                return new Film
                {
                    Titre = product.GetProperty("title").GetString() ?? "Film sans titre",
                    Description = product.GetProperty("description").GetString() ?? "",
                    Genre = GetRandomGenre(),
                    Duree = random.Next(90, 180),
                    ImageUrl = product.GetProperty("thumbnail").GetString() ?? "",
                    DateSortie = DateTime.Now.AddDays(-random.Next(1, 365)),
                    Classification = GetRandomClassification(),
                    Prix = (decimal)(random.NextDouble() * 15 + 5),
                    EstActif = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération du film {id} depuis l'API");
                return new Film();
            }
        }

        public async Task SeedDatabaseAsync()
        {
            try
            {
                // Vérifier si la base de données est déjà peuplée
                if (_context.Films.Any())
                {
                    _logger.LogInformation("Base de données déjà peuplée, pas de nouveau peuplement.");
                    return;
                }

                _logger.LogInformation("Peuplement initial de la base de données...");

                // Créer des salles (toutes avec 200 places)
                var salles = new List<Salle>
                {
                    new Salle { Nom = "Salle 1", NombrePlaces = 200, Description = "Salle principale" },
                    new Salle { Nom = "Salle 2", NombrePlaces = 200, Description = "Salle secondaire" },
                    new Salle { Nom = "Salle 3", NombrePlaces = 200, Description = "Salle VIP" },
                    new Salle { Nom = "Salle 4", NombrePlaces = 200, Description = "Salle intimiste" }
                };

                _context.Salles.AddRange(salles);
                await _context.SaveChangesAsync();

                // Créer les catégories d'âge
                var categoriesAge = new List<CategorieAge>
                {
                    new CategorieAge 
                    { 
                        Nom = "Général", 
                        TrancheAge = "14-64", 
                        Prix = 12.00m, 
                        EstActive = true
                    },
                    new CategorieAge 
                    { 
                        Nom = "Aîné", 
                        TrancheAge = "65+", 
                        Prix = 8.00m,        
                        EstActive = true
                    },
                    new CategorieAge 
                    { 
                        Nom = "Enfant", 
                        TrancheAge = "3-13", 
                        Prix = 6.00m, 
                        EstActive = true
                    }
                };

                _context.CategoriesAge.AddRange(categoriesAge);
                await _context.SaveChangesAsync();

                // Rechercher un fournisseur existant pour assigner les films et séances
                // Prendre le premier utilisateur de type Fournisseur/Organisateur (s'il existe)
                var fournisseur = await _context.Users
                    .FirstOrDefaultAsync(u => u.TypeUtilisateur == "Fournisseur/Organisateur");
                var fournisseurId = fournisseur?.Id; // peut être null si aucun fournisseur

                // Récupérer des films depuis l'API
                var films = await GetFilmsFromApiAsync();
                // Assigner les films au fournisseur s'il existe
                foreach (var film in films)
                {
                    film.FournisseurId = fournisseurId;
                }
                _context.Films.AddRange(films);
                await _context.SaveChangesAsync();

                // Créer des séances
                var seances = new List<Seance>();
                var random = new Random();

                // Créer des séances seulement pour 6 films (films "à l'affiche")
                var filmsAAffiche = films.Take(6).ToList();
                
                foreach (var film in filmsAAffiche)
                {
                    for (int i = 0; i < 3; i++) // 3 séances par film
                    {
                        var salle = salles[random.Next(salles.Count)];
                        var dateHeure = DateTime.Now.AddDays(random.Next(1, 30)).AddHours(random.Next(9, 22));

                        seances.Add(new Seance
                        {
                            FilmId = film.Id,
                            SalleId = salle.Id,
                            DateHeure = dateHeure,
                            Prix = film.Prix,
                            EstActive = true,
                            FournisseurId = fournisseurId
                        });
                    }
                }

                _context.Seances.AddRange(seances);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Base de données peuplée avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du peuplement de la base de données");
            }
        }

        private string GetRandomGenre()
        {
            var genres = new[] { "Action", "Comédie", "Drame", "Horreur", "Science-Fiction", "Romance", "Thriller", "Aventure" };
            return genres[new Random().Next(genres.Length)];
        }

        private string GetRandomClassification()
        {
            var classifications = new[] { "Tous publics", "12+", "16+", "18+" };
            return classifications[new Random().Next(classifications.Length)];
        }

        // Méthode pour mettre à jour toutes les salles existantes à 200 places et recréer les sièges
        public async Task UpdateAllSallesTo200PlacesAsync()
        {
            try
            {
                _logger.LogInformation("Mise à jour de toutes les salles à 200 places et recréation des sièges...");
                
                var salles = await _context.Salles.ToListAsync();
                
                foreach (var salle in salles)
                {
                    // Mettre à jour le nombre de places à 200
                    salle.NombrePlaces = 200;
                    
                    // Récupérer tous les sièges existants pour cette salle
                    var siegesExistants = await _context.Sieges
                        .Where(s => s.SalleId == salle.Id)
                        .ToListAsync();
                    
                    // Compter les sièges réservés (on ne veut pas les supprimer)
                    var siegesReserves = siegesExistants.Count(s => s.EstReserve || s.ReservationId != null);
                    
                    // Supprimer uniquement les sièges non réservés
                    var siegesNonReserves = siegesExistants.Where(s => !s.EstReserve && s.ReservationId == null).ToList();
                    if (siegesNonReserves.Any())
                    {
                        _context.Sieges.RemoveRange(siegesNonReserves);
                        await _context.SaveChangesAsync();
                    }
                    
                    // Compter combien de sièges on a maintenant (après suppression = seulement les réservés)
                    var siegesActuels = await _context.Sieges
                        .Where(s => s.SalleId == salle.Id)
                        .CountAsync();
                    
                    // Créer les sièges manquants pour atteindre 200
                    var siegesACreer = 200 - siegesActuels;
                    if (siegesACreer > 0)
                    {
                        await CreateSiegesForSalle(salle.Id, siegesACreer);
                        _logger.LogInformation($"Salle {salle.Nom}: {siegesACreer} nouveau(x) siège(s) créé(s) (total: 200, réservés conservés: {siegesActuels})");
                    }
                    else
                    {
                        _logger.LogInformation($"Salle {salle.Nom}: Déjà {siegesActuels} sièges (dont {siegesReserves} réservés)");
                    }
                }
                
                await _context.SaveChangesAsync();
                _logger.LogInformation($"{salles.Count} salle(s) mises à jour à 200 places avec succès.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour des salles à 200 places");
                throw;
            }
        }

        // Méthode pour créer un nombre spécifique de sièges pour une salle
        private async Task CreateSiegesForSalle(int salleId, int nombreSiegesACreer)
        {
            var sieges = new List<Siege>();
            var rangs = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T" };
            
            // Pour 200 sièges répartis sur 20 rangs : 10 sièges par rang
            var siegesParRang = 200 / rangs.Length; // 10 sièges par rang
            var resteTotal = 200 % rangs.Length; // 0 pour 200 sièges
            
            // Récupérer les sièges existants pour déterminer les numéros déjà utilisés
            var siegesExistants = await _context.Sieges
                .Where(s => s.SalleId == salleId)
                .ToListAsync();
            
            var siegesCrees = 0;
            
            for (int i = 0; i < rangs.Length && siegesCrees < nombreSiegesACreer; i++)
            {
                var rang = rangs[i];
                var nombreSiegesDansRang = siegesParRang + (i < resteTotal ? 1 : 0);
                
                // Vérifier quels numéros sont déjà utilisés pour ce rang
                var numerosUtilises = siegesExistants
                    .Where(s => s.Rang == rang)
                    .Select(s => s.Numero)
                    .ToHashSet();
                
                for (int j = 1; j <= nombreSiegesDansRang && siegesCrees < nombreSiegesACreer; j++)
                {
                    // Ne créer que si ce numéro n'est pas déjà utilisé
                    if (!numerosUtilises.Contains(j))
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
                        
                        siegesCrees++;
                    }
                }
            }

            if (sieges.Any())
            {
                _context.Sieges.AddRange(sieges);
                await _context.SaveChangesAsync();
            }
        }
    }
}
