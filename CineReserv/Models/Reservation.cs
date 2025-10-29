using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CineReserv.Models
{
    public class Reservation
    {
        public int Id { get; set; }
        
        public int SeanceId { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string NomClient { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string EmailClient { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string TelephoneClient { get; set; } = string.Empty;
        
        public int NombrePlaces { get; set; }
        
        public int CategorieAgeId { get; set; }
        
        public decimal PrixUnitaire { get; set; }
        
        public decimal PrixTotal { get; set; }
        
        public DateTime DateReservation { get; set; } = DateTime.Now;
        
        [StringLength(50)]
        public string Statut { get; set; } = "EnAttente"; // EnAttente, Confirmee, Annulee
        
        [StringLength(200)]
        public string NumeroReservation { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? StripeChargeId { get; set; } // ID de la charge Stripe
        
        // Navigation properties
        [ForeignKey("SeanceId")]
        public virtual Seance Seance { get; set; } = null!;
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
        
        [ForeignKey("CategorieAgeId")]
        public virtual CategorieAge CategorieAge { get; set; } = null!;
    }
}
