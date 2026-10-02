namespace ObsidianSync.Application.Interfaces;

public interface ILocalVaultStorage
{
    Task<IReadOnlyList<FileMetadata>> ScanVaultAsync(string vaultPath, CancellationToken ct);
    Stream OpenRead(string vaultPath, string relativePath);
    Stream OpenWrite(string vaultPath, string relativePath);
    void DeleteFile(string vaultPath, string relativePath);
}
