using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CineReserv.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string Prenom { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string Nom { get; set; } = string.Empty;
        
        // Champs d'inscription simplifiés: suppression des champs d'adresse/date
        
        [Required]
        [StringLength(20)]
        public string TypeUtilisateur { get; set; } = "Client"; // "Client" ou "Fournisseur"
        
        [StringLength(200)]
        public string? NomEntreprise { get; set; } // Pour les fournisseurs
        
        [StringLength(200)]
        public string? DescriptionEntreprise { get; set; } // Pour les fournisseurs
        
        public bool EstActif { get; set; } = true;
        
        public DateTime DateInscription { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
