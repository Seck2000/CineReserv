using CineReserv.Data;
using CineReserv.Models;
using CineReserv.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CineReserv.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        // GET: Auth/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Prenom = model.Prenom,
                    Nom = model.Nom,
                    TypeUtilisateur = model.TypeUtilisateur,
                    DateInscription = DateTime.Now,
                    EstActif = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Utilisateur créé avec succès: {Email}", user.Email);
                    
                    // Connexion automatique après inscription
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    
                    TempData["SuccessMessage"] = "Inscription réussie ! Bienvenue sur CineReserv.";
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // GET: Auth/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, 
                    model.Password, 
                    model.RememberMe, 
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Utilisateur connecté: {Email}", model.Email);
                    
                    // Transférer le panier de session vers l'utilisateur
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        await TransferSessionCartToUser(user.Id);
                    }
                    
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    
                    return RedirectToAction("Index", "Home");
                }

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Compte verrouillé. Veuillez réessayer plus tard.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Email ou mot de passe incorrect.");
                }
            }

            return View(model);
        }

        // POST: Auth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Utilisateur déconnecté");
            return RedirectToAction("Index", "Home");
        }

        // GET: Auth/Profile
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            return View(user);
        }

        // GET: Auth/EditProfile
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var model = new EditProfileViewModel
            {
                Prenom = user.Prenom,
                Nom = user.Nom,
                Email = user.Email!,
                TypeUtilisateur = user.TypeUtilisateur
            };

            return View(model);
        }

        // POST: Auth/EditProfile
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                user.Prenom = model.Prenom;
                user.Nom = model.Nom;
                user.Email = model.Email;
                user.UserName = model.Email; // Mettre à jour aussi le UserName
                user.TypeUtilisateur = model.TypeUtilisateur;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Profil mis à jour avec succès !";
                    return RedirectToAction("Profile");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // Transférer le panier de session vers l'utilisateur
        private async Task TransferSessionCartToUser(string userId)
        {
            try
            {
                var sessionId = HttpContext.Session.GetString("SessionId");
                if (string.IsNullOrEmpty(sessionId)) return;

                // Récupérer les articles du panier de session
                var sessionItems = await _context.PanierItems
                    .Where(p => p.SessionId == sessionId && p.UserId == null)
                    .ToListAsync();

                if (sessionItems.Any())
                {
                    // Transférer vers l'utilisateur
                    foreach (var item in sessionItems)
                    {
                        item.UserId = userId;
                        item.SessionId = string.Empty; // Nettoyer la session
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Panier transféré de session vers utilisateur {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du transfert du panier de session");
            }
        }
    }
}
