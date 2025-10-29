using System.ComponentModel.DataAnnotations;

namespace CineReserv.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        
        [Required]
        public string NumeroFacture { get; set; } = string.Empty;
        
        [Required]
        public int ReservationId { get; set; }
        public virtual Reservation Reservation { get; set; } = null!;
        
        [Required]
        public string ClientId { get; set; } = string.Empty;
        public virtual ApplicationUser Client { get; set; } = null!;
        
        [Required]
        public string FournisseurId { get; set; } = string.Empty;
        public virtual ApplicationUser Fournisseur { get; set; } = null!;
        
        [Required]
        [DataType(DataType.Currency)]
        public decimal Montant { get; set; }
        
        [Required]
        public DateTime DateFacture { get; set; } = DateTime.Now;
        
        [Required]
        public string Statut { get; set; } = "Payée";
        
        public string? PaymentIntentId { get; set; }
        
        public string? PdfUrl { get; set; }
        
        public string? HtmlContent { get; set; }
        
        [Required]
        [StringLength(200)]
        public string NomClient { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string EmailClient { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? AdresseClient { get; set; }
        
        [StringLength(10)]
        public string? CodePostalClient { get; set; }
        
        [StringLength(100)]
        public string? VilleClient { get; set; }
        
        [Required]
        [StringLength(200)]
        public string NomFournisseur { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string EmailFournisseur { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? AdresseFournisseur { get; set; }
        
        [StringLength(10)]
        public string? CodePostalFournisseur { get; set; }
        
        [StringLength(100)]
        public string? VilleFournisseur { get; set; }
    }
}