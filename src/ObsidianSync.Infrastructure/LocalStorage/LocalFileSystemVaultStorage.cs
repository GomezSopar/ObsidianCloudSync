namespace ObsidianSync.Infrastructure.LocalStorage;

using System.Security.Cryptography;
using ObsidianSync.Application.Interfaces;
using ObsidianSync.Domain.ValueObjects;

public class LocalFileSystemVaultStorage : ILocalVaultStorage
{
    private static readonly HashSet<string> IgnoredFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ".obsidian/workspace.json",
        ".obsidian/workspace-mobile.json",
        ".DS_Store",
        "Thumbs.db"
    };

    public async Task<IReadOnlyList<FileMetadata>> ScanVaultAsync(string vaultPath, CancellationToken ct)
    {
        if (!Directory.Exists(vaultPath))
        {
            Directory.CreateDirectory(vaultPath);
            return Array.Empty<FileMetadata>();
        }

        var list = new List<FileMetadata>();
        var files = Directory.EnumerateFiles(vaultPath, "*", SearchOption.AllDirectories);

        foreach (var fullPath in files)
        {
            ct.ThrowIfCancellationRequested();
            var relPath = Path.GetRelativePath(vaultPath, fullPath).Replace('\\', '/');

            if (IgnoredFiles.Contains(relPath) || relPath.StartsWith(".git/", StringComparison.OrdinalIgnoreCase))
                continue;

            var md5 = await CalculateMd5Async(fullPath, ct);
            var modifiedUtc = File.GetLastWriteTimeUtc(fullPath);

            list.Add(new FileMetadata(relPath, null, new FileHash(md5), modifiedUtc));
        }

        return list;
    }

    public Stream OpenRead(string vaultPath, string relativePath)
    {
        var fullPath = Path.Combine(vaultPath, relativePath);
        return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

    public Stream OpenWrite(string vaultPath, string relativePath)
    {
        var fullPath = Path.Combine(vaultPath, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        return new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
    }

    public void DeleteFile(string vaultPath, string relativePath)
    {
        var fullPath = Path.Combine(vaultPath, relativePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    private static async Task<string> CalculateMd5Async(string filePath, CancellationToken ct)
    {
        using var md5 = MD5.Create();
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var hashBytes = await md5.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
