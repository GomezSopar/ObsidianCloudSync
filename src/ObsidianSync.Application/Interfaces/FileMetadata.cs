namespace ObsidianSync.Application.Interfaces;

using ObsidianSync.Domain.ValueObjects;

public record FileMetadata(
    string RelativePath,
    string? RemoteId,
    FileHash Hash,
    DateTime LastModifiedUtc
);
