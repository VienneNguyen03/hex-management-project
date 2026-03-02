using Microsoft.EntityFrameworkCore;
using HexManager.Models;

namespace HexManager.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<TrafficSignal> TrafficSignals { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TrafficSignal>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Create index on HexAddress for faster lookups and uniqueness check
            entity.HasIndex(e => e.HexAddress)
                  .IsUnique()
                  .HasDatabaseName("IX_TrafficSignals_HexAddress");
            
            // Additional indexes for common queries
            entity.HasIndex(e => e.Boro)
                  .HasDatabaseName("IX_TrafficSignals_Boro");
            
            entity.HasIndex(e => new { e.StreetName1, e.StreetName2 })
                  .HasDatabaseName("IX_TrafficSignals_Streets");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}
