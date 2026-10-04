namespace ObsidianSync.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using ObsidianSync.Domain.Entities;
using ObsidianSync.Domain.ValueObjects;

public class VaultDbContext : DbContext
{
    public DbSet<Vault> Vaults => Set<Vault>();
    public DbSet<SyncedFileState> SyncedFiles => Set<SyncedFileState>();

    public VaultDbContext(DbContextOptions<VaultDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vault>(b =>
        {
            b.HasKey(v => v.Id);
            b.HasMany(v => v.Files)
             .WithOne()
             .OnDelete(DeleteBehavior.Cascade);

            b.Navigation(v => v.Files).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<SyncedFileState>(b =>
        {
            b.HasKey(f => f.Id);
            b.Property(f => f.LastSyncedHash)
             .HasConversion(
                 h => h.Value,
                 v => new FileHash(v));
        });
    }
}
