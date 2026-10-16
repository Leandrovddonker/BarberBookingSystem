using BarberBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BarberBookingSystem.Data;

public class BarberSystemDbContext(DbContextOptions<BarberSystemDbContext> options) : DbContext(options)
{
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Barber> Barbers { get; set; }
    public DbSet<OpeningHours> OpeningHours { get; set; }
    public DbSet<HairCut> HairCuts { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(entity.GetTableName()?.ToLower());
        }

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Reservation>()
            .HasOne(r => r.User)
            .WithMany(u => u.Reservations)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reservation>()
            .HasOne(r => r.HairCut)
            .WithMany(h => h.Reservations)
            .HasForeignKey(r => r.HaircutId)
            .OnDelete(DeleteBehavior.NoAction);
        
        modelBuilder.Entity<Reservation>()
            .HasOne(r => r.Barber)
            .WithMany(b => b.Reservations)
            .HasForeignKey(r => r.BarberId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Barber>()
            .HasOne(b => b.User)
            .WithOne(u => u.Barber)
            .HasForeignKey<Barber>(b => b.UserId);

        modelBuilder.Entity<User>();
    }

}