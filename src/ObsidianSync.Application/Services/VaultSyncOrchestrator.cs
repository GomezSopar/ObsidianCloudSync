namespace ObsidianSync.Application.Services;

using ObsidianSync.Application.Interfaces;
using ObsidianSync.Domain.Entities;
using ObsidianSync.Domain.Repositories;
using ObsidianSync.Domain.ValueObjects;

public record SyncSummary(int Uploaded, int Downloaded, int Deleted, int Conflicts, DateTime FinishedAtUtc);

public class VaultSyncOrchestrator
{
    private readonly ICloudStorageProvider _driveStorage;
    private readonly ILocalVaultStorage _localStorage;
    private readonly IVaultRepository _vaultRepo;

    public VaultSyncOrchestrator(
        ICloudStorageProvider driveStorage,
        ILocalVaultStorage localStorage,
        IVaultRepository vaultRepo)
    {
        _driveStorage = driveStorage;
        _localStorage = localStorage;
        _vaultRepo = vaultRepo;
    }

    public async Task<SyncSummary> RunSyncAsync(Vault vault, CancellationToken ct = default)
    {
        int uploaded = 0, downloaded = 0, deleted = 0, conflicts = 0;

        var localMap = (await _localStorage.ScanVaultAsync(vault.LocalPath, ct))
            .ToDictionary(f => f.RelativePath, StringComparer.OrdinalIgnoreCase);

        var remoteMap = (await _driveStorage.ListFilesAsync(vault.RemoteFolderId, ct))
            .ToDictionary(f => f.RelativePath, StringComparer.OrdinalIgnoreCase);

        var allPaths = localMap.Keys
            .Union(remoteMap.Keys, StringComparer.OrdinalIgnoreCase)
            .Union(vault.Files.Select(f => f.RelativePath), StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var path in allPaths)
        {
            ct.ThrowIfCancellationRequested();

            localMap.TryGetValue(path, out var localFile);
            remoteMap.TryGetValue(path, out var remoteFile);

            var action = vault.EvaluateFileSync(path, localFile?.Hash, remoteFile?.Hash);

            switch (action)
            {
                case SyncAction.UploadToRemote:
                    if (localFile is not null)
                    {
                        await using var readStream = _localStorage.OpenRead(vault.LocalPath, path);
                        var remoteId = await _driveStorage.UploadOrUpdateAsync(
                            vault.RemoteFolderId, path, readStream, remoteFile?.RemoteId, ct);
                        vault.MarkAsSynced(path, localFile.Hash, remoteId);
                        uploaded++;
                    }
                    break;

                case SyncAction.DownloadToLocal:
                    if (remoteFile?.RemoteId is not null)
                    {
                        await using (var writeStream = _localStorage.OpenWrite(vault.LocalPath, path))
                        {
                            await _driveStorage.DownloadAsync(remoteFile.RemoteId, writeStream, ct);
                        }
                        vault.MarkAsSynced(path, remoteFile.Hash, remoteFile.RemoteId);
                        downloaded++;
                    }
                    break;

                case SyncAction.DeleteLocal:
                    _localStorage.DeleteFile(vault.LocalPath, path);
                    vault.UntrackFile(path);
                    deleted++;
                    break;

                case SyncAction.DeleteRemote:
                    if (remoteFile?.RemoteId is not null)
                    {
                        await _driveStorage.DeleteAsync(remoteFile.RemoteId, ct);
                    }
                    vault.UntrackFile(path);
                    deleted++;
                    break;

                case SyncAction.Conflict:
                    conflicts++;
                    break;
            }
        }

        await _vaultRepo.SaveChangesAsync(vault, ct);
        return new SyncSummary(uploaded, downloaded, deleted, conflicts, DateTime.UtcNow);
    }
}
