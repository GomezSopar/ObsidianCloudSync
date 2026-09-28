namespace ObsidianSync.Domain.Repositories;

using ObsidianSync.Domain.Entities;

public interface IVaultRepository
{
    Task<Vault> GetOrCreateDefaultAsync(CancellationToken ct = default);
    Task SaveChangesAsync(Vault vault, CancellationToken ct = default);
}
