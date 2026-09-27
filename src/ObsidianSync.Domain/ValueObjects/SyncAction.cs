namespace ObsidianSync.Domain.ValueObjects;

public enum SyncAction
{
    None = 0,
    UploadToRemote,
    DownloadToLocal,
    DeleteLocal,
    DeleteRemote,
    Conflict
}
