using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CineReserv.Models
{
    public class Siege
    {
        public int Id { get; set; }

        [Required]
        public int SalleId { get; set; }

        [Required]
        [StringLength(10)]
        public string Rang { get; set; } = string.Empty; // Ex: "A", "B", "C", etc.

        [Required]
        public int Numero { get; set; } // Ex: 1, 2, 3, etc.

        [Required]
        [StringLength(20)]
        public string Type { get; set; } = "Standard"; // Standard, VIP, Handicap

        public bool EstOccupe { get; set; } = false;

        public bool EstReserve { get; set; } = false;

        public DateTime? DateReservation { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        // Navigation properties
        [ForeignKey("SalleId")]
        public virtual Salle Salle { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // Propriété calculée pour l'affichage
        [NotMapped]
        public string NomComplet => $"{Rang}{Numero}"; // Ex: "A1", "B5", etc.
    }
}

