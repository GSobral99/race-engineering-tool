using Microsoft.EntityFrameworkCore;
using RaceEngineeringApi.Models;

namespace RaceEngineeringApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Stint> Stints => Set<Stint>();
    public DbSet<Lap> Laps => Set<Lap>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Session>()
            .HasMany(s => s.Stints)
            .WithOne(st => st.Session)
            .HasForeignKey(st => st.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Stint>()
            .HasMany(st => st.Laps)
            .WithOne(l => l.Stint)
            .HasForeignKey(l => l.StintId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
