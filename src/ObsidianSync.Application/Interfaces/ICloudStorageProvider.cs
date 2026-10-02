namespace ObsidianSync.Application.Interfaces;

public interface ICloudStorageProvider
{
    Task<IReadOnlyList<FileMetadata>> ListFilesAsync(string folderId, CancellationToken ct);
    Task<string> UploadOrUpdateAsync(string folderId, string relativePath, Stream content, string? existingRemoteId, CancellationToken ct);
    Task DownloadAsync(string remoteFileId, Stream destination, CancellationToken ct);
    Task DeleteAsync(string remoteFileId, CancellationToken ct);
}
