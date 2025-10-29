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
        
        public DateTime DateNaissance { get; set; }
        
        [StringLength(200)]
        public string Adresse { get; set; } = string.Empty;
        
        [StringLength(10)]
        public string CodePostal { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string Ville { get; set; } = string.Empty;
        
        [Required]
        [StringLength(30)]
        public string TypeUtilisateur { get; set; } = "Client"; // "Client" ou "Fournisseur/Organisateur"
        
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
