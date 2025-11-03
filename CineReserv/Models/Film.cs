using System.ComponentModel.DataAnnotations;

namespace CineReserv.Models
{
    public class Film
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Titre { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string Genre { get; set; } = string.Empty;
        
        public int Duree { get; set; } // en minutes
        
        [StringLength(200)]
        public string ImageUrl { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string BandeAnnonceUrl { get; set; } = string.Empty;
        
        public DateTime DateSortie { get; set; }
        
        [StringLength(50)]
        public string Classification { get; set; } = string.Empty; // PG, PG-13, R, etc.
        
        public decimal Prix { get; set; }
        
        public bool EstActif { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<Seance> Seances { get; set; } = new List<Seance>();
    }
}
