using CineReserv.Models;

namespace CineReserv.Models.ViewModels
{
    public class StatistiquesFacturationViewModel
    {
        public decimal TotalRevenus { get; set; }
        public int NombreFactures { get; set; }
        public decimal RevenusMoyens { get; set; }
        public decimal RevenusCeMois { get; set; }
        public decimal RevenusCeMoisDernier { get; set; }
        public List<Facture> FacturesRecent { get; set; } = new List<Facture>();
    }
}

