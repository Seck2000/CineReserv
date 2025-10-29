using CineReserv.Data;
using CineReserv.Models;
using CineReserv.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Globalization;


var builder = WebApplication.CreateBuilder(args);

// Configuration Stripe
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

// Configuration de la culture pour afficher les prix en dollars
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("en-US") };
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    )
);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// Configuration de la redirection de connexion
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
});

// Enregistrer HttpClient et ApiService
builder.Services.AddHttpClient<IApiService, ApiService>();

// Enregistrer le service de session
builder.Services.AddScoped<ISessionService, SessionService>();

// Configuration des sessions
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Peuplement automatique de la base de données au démarrage
using (var scope = app.Services.CreateScope())
{
    var apiService = scope.ServiceProvider.GetRequiredService<IApiService>();
    await apiService.SeedDatabaseAsync();
    
    // Vider toutes les sessions au démarrage
    var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();
    await sessionService.ClearAllSessionsAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Configuration de la gestion des erreurs 404
app.UseStatusCodePagesWithReExecute("/Home/NotFound");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Configuration de la localisation pour les devises
app.UseRequestLocalization();

app.UseSession();
app.UseAuthentication();   
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
