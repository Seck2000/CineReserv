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

                // Créer des salles
                var salles = new List<Salle>
                {
                    new Salle { Nom = "Salle 1", NombrePlaces = 100, Description = "Salle principale" },
                    new Salle { Nom = "Salle 2", NombrePlaces = 80, Description = "Salle secondaire" },
                    new Salle { Nom = "Salle 3", NombrePlaces = 120, Description = "Salle VIP" },
                    new Salle { Nom = "Salle 4", NombrePlaces = 60, Description = "Salle intimiste" }
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

                // Récupérer des films depuis l'API
                var films = await GetFilmsFromApiAsync();
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
                            EstActive = true
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

        // Méthode pour forcer le peuplement (utile pour le développement)
        public async Task ForceSeedDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Forçage du peuplement de la base de données...");
                
                // Vider la base de données existante
                _context.PanierItems.RemoveRange(_context.PanierItems);
                _context.Reservations.RemoveRange(_context.Reservations);
                _context.Sieges.RemoveRange(_context.Sieges);
                _context.Seances.RemoveRange(_context.Seances);
                _context.Films.RemoveRange(_context.Films);
                _context.Salles.RemoveRange(_context.Salles);
                _context.CategoriesAge.RemoveRange(_context.CategoriesAge);
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("Base de données vidée avec succès");

                // Peupler avec de nouvelles données
                await SeedDatabaseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du forçage du peuplement de la base de données");
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
    }
}
