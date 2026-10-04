namespace ObsidianSync.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using ObsidianSync.Domain.Entities;
using ObsidianSync.Domain.Repositories;

public class EfVaultRepository : IVaultRepository
{
    private readonly VaultDbContext _db;
    private readonly string _defaultName;
    private readonly string _defaultLocalPath;
    private readonly string _defaultRemoteFolderId;

    public EfVaultRepository(
        VaultDbContext db,
        string defaultName,
        string defaultLocalPath,
        string defaultRemoteFolderId)
    {
        _db = db;
        _defaultName = defaultName;
        _defaultLocalPath = defaultLocalPath;
        _defaultRemoteFolderId = defaultRemoteFolderId;
    }

    public async Task<Vault> GetOrCreateDefaultAsync(CancellationToken ct = default)
    {
        await _db.Database.EnsureCreatedAsync(ct);

        var vault = await _db.Vaults
            .Include(v => v.Files)
            .FirstOrDefaultAsync(ct);

        if (vault is null)
        {
            vault = new Vault(Guid.NewGuid(), _defaultName, _defaultLocalPath, _defaultRemoteFolderId);
            _db.Vaults.Add(vault);
            await _db.SaveChangesAsync(ct);
        }

        return vault;
    }

    public async Task SaveChangesAsync(Vault vault, CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
