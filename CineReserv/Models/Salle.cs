using System.ComponentModel.DataAnnotations;

namespace CineReserv.Models
{
    public class Salle
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Nom { get; set; } = string.Empty;
        
        public int NombrePlaces { get; set; }
        
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;
        
        public bool EstActive { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<Seance> Seances { get; set; } = new List<Seance>();
    }
}
