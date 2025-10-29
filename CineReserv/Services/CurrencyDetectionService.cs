using System.Net.Http.Json;
using System.Text.Json;

namespace CineReserv.Services
{
    public class CurrencyDetectionService : ICurrencyDetectionService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CurrencyDetectionService> _logger;

        // Mapping pays -> devise Stripe
        private static readonly Dictionary<string, string> CountryToCurrency = new()
        {
            { "US", "usd" },
            { "CA", "usd" },
            { "FR", "eur" },
            { "BE", "eur" },
            { "CH", "chf" },
            { "DE", "eur" },
            { "ES", "eur" },
            { "IT", "eur" },
            { "NL", "eur" },
            { "GB", "gbp" },
            { "UK", "gbp" },
            { "AU", "aud" },
            { "NZ", "nzd" },
            { "JP", "jpy" },
            { "CN", "cny" },
            { "IN", "inr" },
            { "BR", "brl" },
            { "MX", "mxn" },
            { "AR", "ars" },
            { "ZA", "zar" },
            { "SE", "sek" },
            { "NO", "nok" },
            { "DK", "dkk" },
            { "PL", "pln" },
            { "CZ", "czk" },
            { "HU", "huf" },
            { "RO", "ron" },
            { "RU", "rub" },
            { "TR", "try" },
            { "IL", "ils" },
            { "AE", "aed" },
            { "SA", "sar" },
            { "SG", "sgd" },
            { "HK", "hkd" },
            { "KR", "krw" },
            { "TH", "thb" },
            { "MY", "myr" },
            { "ID", "idr" },
            { "PH", "php" },
            { "VN", "vnd" }
        };

        public CurrencyDetectionService(HttpClient httpClient, ILogger<CurrencyDetectionService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(5); // Timeout court pour éviter d'attendre trop longtemps
        }

        public async Task<string> DetectCurrencyAsync(string? clientIp = null)
        {
            try
            {
                // Si pas d'IP fournie, retourner USD par défaut
                if (string.IsNullOrWhiteSpace(clientIp) || 
                    clientIp == "127.0.0.1" || 
                    clientIp == "::1" ||
                    clientIp.StartsWith("192.168.") ||
                    clientIp.StartsWith("10.") ||
                    clientIp.StartsWith("172."))
                {
                    _logger.LogInformation("IP locale ou manquante détectée, utilisation de USD par défaut");
                    return "usd";
                }

                // Utiliser l'API ipapi.co pour détecter le pays (gratuite jusqu'à 1000 requêtes/mois)
                // Format: https://ipapi.co/{ip}/json/
                var response = await _httpClient.GetAsync($"https://ipapi.co/{clientIp}/json/");
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                    
                    if (jsonDoc.RootElement.TryGetProperty("country_code", out var countryCodeElement))
                    {
                        var countryCode = countryCodeElement.GetString();
                        
                        if (!string.IsNullOrWhiteSpace(countryCode) && 
                            CountryToCurrency.TryGetValue(countryCode.ToUpperInvariant(), out var currency))
                        {
                            _logger.LogInformation("Devise détectée: {Currency} pour le pays {Country}", currency, countryCode);
                            return currency;
                        }
                    }

                    // Si on a pas trouvé de devise pour le pays, essayer de récupérer la devise directement
                    if (jsonDoc.RootElement.TryGetProperty("currency", out var currencyElement))
                    {
                        var currency = currencyElement.GetString();
                        if (!string.IsNullOrWhiteSpace(currency))
                        {
                            var currencyLower = currency.ToLowerInvariant();
                            _logger.LogInformation("Devise détectée via API: {Currency}", currencyLower);
                            return currencyLower;
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Erreur lors de la détection de devise via IP API");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Timeout lors de la détection de devise");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de la détection de devise");
            }

            // Fallback: USD par défaut
            _logger.LogInformation("Utilisation de USD par défaut (devise non détectée)");
            return "usd";
        }
    }
}

