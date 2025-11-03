using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CineReserv.Models
{
    public class Seance
    {
        public int Id { get; set; }
        
        public int FilmId { get; set; }
        public int SalleId { get; set; }
        
        // Identifiant du fournisseur/organisateur propriétaire de la séance
        [StringLength(450)]
        public string? FournisseurId { get; set; }
        
        public DateTime DateHeure { get; set; }
        
        public decimal Prix { get; set; }
        
        public bool EstActive { get; set; } = true;
        
        // Navigation properties
        [ForeignKey("FilmId")]
        public virtual Film Film { get; set; } = null!;
        
        [ForeignKey("SalleId")]
        public virtual Salle Salle { get; set; } = null!;
        
        [ForeignKey("FournisseurId")]
        public virtual ApplicationUser? Fournisseur { get; set; }
        
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
