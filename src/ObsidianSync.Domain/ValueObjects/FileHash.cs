namespace ObsidianSync.Domain.ValueObjects;

public readonly record struct FileHash(string Value)
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value;
}
