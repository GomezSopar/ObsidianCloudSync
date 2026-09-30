namespace ObsidianSync.Domain.Entities;

using ObsidianSync.Domain.ValueObjects;

public class Vault
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string LocalPath { get; private set; } = string.Empty;
    public string RemoteFolderId { get; private set; } = string.Empty;
    public DateTime? LastSyncAttemptUtc { get; private set; }

    private readonly List<SyncedFileState> _files = new();
    public IReadOnlyCollection<SyncedFileState> Files => _files.AsReadOnly();

    private Vault() { }

    public Vault(Guid id, string name, string localPath, string remoteFolderId)
    {
        Id = id;
        Name = name;
        LocalPath = localPath;
        RemoteFolderId = remoteFolderId;
    }

    public SyncAction EvaluateFileSync(
        string relativePath,
        FileHash? localHash,
        FileHash? remoteHash)
    {
        var tracked = _files.FirstOrDefault(f =>
            string.Equals(f.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase));

        if (localHash == remoteHash)
            return SyncAction.None;

        if (tracked is null)
        {
            if (localHash is not null && remoteHash is null)
                return SyncAction.UploadToRemote;
            if (remoteHash is not null && localHash is null)
                return SyncAction.DownloadToLocal;

            return SyncAction.Conflict;
        }

        if (localHash != tracked.LastSyncedHash && remoteHash == tracked.LastSyncedHash)
            return localHash is null ? SyncAction.DeleteRemote : SyncAction.UploadToRemote;

        if (remoteHash != tracked.LastSyncedHash && localHash == tracked.LastSyncedHash)
            return remoteHash is null ? SyncAction.DeleteLocal : SyncAction.DownloadToLocal;

        return SyncAction.Conflict;
    }

    public void MarkAsSynced(string relativePath, FileHash hash, string remoteFileId)
    {
        var existing = _files.FirstOrDefault(f =>
            string.Equals(f.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.UpdateState(hash, remoteFileId, DateTime.UtcNow);
        }
        else
        {
            _files.Add(new SyncedFileState(relativePath, remoteFileId, hash, DateTime.UtcNow));
        }
        LastSyncAttemptUtc = DateTime.UtcNow;
    }

    public void UntrackFile(string relativePath)
    {
        var existing = _files.FirstOrDefault(f =>
            string.Equals(f.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            _files.Remove(existing);

        LastSyncAttemptUtc = DateTime.UtcNow;
    }
}
