using System.ComponentModel.DataAnnotations;

namespace CineReserv.Models
{
    public class CategorieAge
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Nom { get; set; } = string.Empty; // "Adulte", "Étudiant", "Enfant", "Senior"
        
        [Required]
        [StringLength(20)]
        public string TrancheAge { get; set; } = string.Empty; // "18+", "16-17", "12-15", "0-11"
        
        public decimal Prix { get; set; }
        
        public bool EstActive { get; set; } = true;
        
        [StringLength(200)]
        public string? Description { get; set; }
    }
}

