using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CineReserv.Models;

namespace CineReserv.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Film> Films { get; set; }
        public DbSet<Salle> Salles { get; set; }
        public DbSet<Seance> Seances { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<PanierItem> PanierItems { get; set; }
        public DbSet<CategorieAge> CategoriesAge { get; set; }
        public DbSet<Siege> Sieges { get; set; } // Added for seat management
        public DbSet<Facture> Factures { get; set; } // Added for billing

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configuration des relations
            builder.Entity<Seance>()
                .HasOne(s => s.Film)
                .WithMany(f => f.Seances)
                .HasForeignKey(s => s.FilmId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Seance>()
                .HasOne(s => s.Salle)
                .WithMany(s => s.Seances)
                .HasForeignKey(s => s.SalleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Reservation>()
                .HasOne(r => r.Seance)
                .WithMany(s => s.Reservations)
                .HasForeignKey(r => r.SeanceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Reservation>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PanierItem>()
                .HasOne(p => p.Seance)
                .WithMany()
                .HasForeignKey(p => p.SeanceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PanierItem>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PanierItem>()
                .HasOne(p => p.CategorieAge)
                .WithMany()
                .HasForeignKey(p => p.CategorieAgeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Reservation>()
                .HasOne(r => r.CategorieAge)
                .WithMany()
                .HasForeignKey(r => r.CategorieAgeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configuration des relations pour Siege
            builder.Entity<Siege>()
                .HasOne(s => s.Salle)
                .WithMany()
                .HasForeignKey(s => s.SalleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Siege>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configuration des relations pour Facture
            builder.Entity<Facture>()
                .HasOne(i => i.Reservation)
                .WithMany()
                .HasForeignKey(i => i.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Facture>()
                .HasOne(i => i.Client)
                .WithMany()
                .HasForeignKey(i => i.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Facture>()
                .HasOne(i => i.Fournisseur)
                .WithMany()
                .HasForeignKey(i => i.FournisseurId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configuration des relations pour Film
            builder.Entity<Film>()
                .HasOne(f => f.Fournisseur)
                .WithMany()
                .HasForeignKey(f => f.FournisseurId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Seance>()
                .HasOne(s => s.Fournisseur)
                .WithMany()
                .HasForeignKey(s => s.FournisseurId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
