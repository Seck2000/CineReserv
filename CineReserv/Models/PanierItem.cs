using System.ComponentModel.DataAnnotations;

namespace CineReserv.Models
{
    public class PanierItem
    {
        public int Id { get; set; }
        
        public int SeanceId { get; set; }
        
        [Required]
        public string SessionId { get; set; } = string.Empty; // Pour les utilisateurs non connectés
        
        public string? UserId { get; set; } // Pour les utilisateurs connectés
        
        public int NombrePlaces { get; set; }
        
        public int CategorieAgeId { get; set; } // Added for age category
        
        public int Quantite { get; set; } // Added for quantity per category
        
        public decimal PrixUnitaire { get; set; }
        
        public decimal PrixTotal => Quantite * PrixUnitaire; // Updated to use Quantite
        
        public DateTime DateAjout { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual Seance Seance { get; set; } = null!;
        public virtual ApplicationUser? User { get; set; }
        public virtual CategorieAge CategorieAge { get; set; } = null!; // Added for age category navigation
    }
}
