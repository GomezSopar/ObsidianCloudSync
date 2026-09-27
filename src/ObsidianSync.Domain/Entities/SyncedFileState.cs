namespace ObsidianSync.Domain.Entities;

using ObsidianSync.Domain.ValueObjects;

public class SyncedFileState
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string RelativePath { get; private set; } = string.Empty;
    public string RemoteFileId { get; private set; } = string.Empty;
    public FileHash LastSyncedHash { get; private set; }
    public DateTime LastSyncedUtc { get; private set; }

    private SyncedFileState() { }

    public SyncedFileState(string relativePath, string remoteFileId, FileHash hash, DateTime syncedUtc)
    {
        RelativePath = relativePath;
        RemoteFileId = remoteFileId;
        LastSyncedHash = hash;
        LastSyncedUtc = syncedUtc;
    }

    public void UpdateState(FileHash hash, string remoteFileId, DateTime syncedUtc)
    {
        LastSyncedHash = hash;
        RemoteFileId = remoteFileId;
        LastSyncedUtc = syncedUtc;
    }
}
