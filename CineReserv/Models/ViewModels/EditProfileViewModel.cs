using System.ComponentModel.DataAnnotations;

namespace CineReserv.Models.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est obligatoire")]
        [StringLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "L'email n'est pas valide")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le type d'utilisateur est obligatoire")]
        public string TypeUtilisateur { get; set; } = string.Empty;
    }
}


