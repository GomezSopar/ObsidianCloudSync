namespace ObsidianSync.Infrastructure.GoogleDrive;

using Google.Apis.Drive.v3;
using ObsidianSync.Application.Interfaces;
using ObsidianSync.Domain.ValueObjects;

public class GoogleDriveStorageProvider : ICloudStorageProvider
{
    private readonly DriveService _drive;

    public GoogleDriveStorageProvider(DriveService drive)
    {
        _drive = drive;
    }

    public async Task<IReadOnlyList<FileMetadata>> ListFilesAsync(string folderId, CancellationToken ct)
    {
        var req = _drive.Files.List();
        req.Q = "'" + folderId + "' in parents and trashed = false";
        req.Fields = "files(id, name, md5Checksum, modifiedTime)";
        req.PageSize = 1000;

        var res = await req.ExecuteAsync(ct);
        if (res.Files is null)
            return Array.Empty<FileMetadata>();

        return res.Files.Select(f => new FileMetadata(
            f.Name,
            f.Id,
            new FileHash(f.Md5Checksum ?? string.Empty),
            f.ModifiedTimeDateTimeOffset?.UtcDateTime ?? DateTime.UtcNow
        )).ToList();
    }

    public async Task<string> UploadOrUpdateAsync(
        string folderId,
        string relativePath,
        Stream content,
        string? existingRemoteId,
        CancellationToken ct)
    {
        var contentType = relativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            ? "text/markdown"
            : "application/octet-stream";

        if (!string.IsNullOrWhiteSpace(existingRemoteId))
        {
            var meta = new Google.Apis.Drive.v3.Data.File { Name = relativePath };
            var updateReq = _drive.Files.Update(meta, existingRemoteId, content, contentType);
            updateReq.Fields = "id";
            var progress = await updateReq.UploadAsync(ct);

            if (progress.Status != Google.Apis.Upload.UploadStatus.Completed)
                throw new IOException("Fallo actualizando archivo en Drive (" + relativePath + "): " + progress.Exception?.Message);

            return updateReq.ResponseBody.Id;
        }

        var createMeta = new Google.Apis.Drive.v3.Data.File
        {
            Name = relativePath,
            Parents = new List<string> { folderId }
        };

        var createReq = _drive.Files.Create(createMeta, content, contentType);
        createReq.Fields = "id";
        var createProgress = await createReq.UploadAsync(ct);

        if (createProgress.Status != Google.Apis.Upload.UploadStatus.Completed)
            throw new IOException("Fallo subiendo archivo a Drive (" + relativePath + "): " + createProgress.Exception?.Message);

        return createReq.ResponseBody.Id;
    }

    public async Task DownloadAsync(string remoteFileId, Stream destination, CancellationToken ct)
    {
        var req = _drive.Files.Get(remoteFileId);
        await req.DownloadAsync(destination, ct);
    }

    public async Task DeleteAsync(string remoteFileId, CancellationToken ct)
    {
        await _drive.Files.Delete(remoteFileId).ExecuteAsync(ct);
    }
}
