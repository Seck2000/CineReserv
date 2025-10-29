using System.ComponentModel.DataAnnotations;

namespace CineReserv.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Le prénom est obligatoire")]
        [StringLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
        [Display(Name = "Prénom")]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        [Display(Name = "Nom")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire")]
        [StringLength(100, ErrorMessage = "Le mot de passe doit contenir entre {2} et {1} caractères", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmation du mot de passe est obligatoire")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le mot de passe")]
        [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le type d'utilisateur est obligatoire")]
        [Display(Name = "Type d'utilisateur")]
        public string TypeUtilisateur { get; set; } = "Client";

        [Required(ErrorMessage = "L'adresse est obligatoire")]
        [StringLength(200, ErrorMessage = "L'adresse ne peut pas dépasser 200 caractères")]
        [Display(Name = "Adresse")]
        public string Adresse { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le code postal est obligatoire")]
        [StringLength(10, ErrorMessage = "Le code postal ne peut pas dépasser 10 caractères")]
        [Display(Name = "Code postal")]
        public string CodePostal { get; set; } = string.Empty;

        [Required(ErrorMessage = "La ville est obligatoire")]
        [StringLength(100, ErrorMessage = "La ville ne peut pas dépasser 100 caractères")]
        [Display(Name = "Ville")]
        public string Ville { get; set; } = string.Empty;

        [Required(ErrorMessage = "La date de naissance est obligatoire")]
        [Display(Name = "Date de naissance")]
        public DateTime DateNaissance { get; set; } = DateTime.Now.AddYears(-18);
    }
}