namespace ObsidianSync.UnitTests;

using ObsidianSync.Domain.Entities;
using ObsidianSync.Domain.ValueObjects;
using Xunit;

public class VaultSyncDecisionTests
{
    [Fact]
    public void EvaluateFileSync_NewLocalFile_ShouldReturnUpload()
    {
        var vault = new Vault(Guid.NewGuid(), "UOC-Notes", "C:/Notes", "folder-1");
        var action = vault.EvaluateFileSync("Semestre1/Algebra.md", new FileHash("abc123"), null);

        Assert.Equal(SyncAction.UploadToRemote, action);
    }

    [Fact]
    public void EvaluateFileSync_BothModifiedDifferently_ShouldReturnConflict()
    {
        var vault = new Vault(Guid.NewGuid(), "UOC-Notes", "C:/Notes", "folder-1");
        var initialHash = new FileHash("111111");
        vault.MarkAsSynced("Trabajo/TCServi-Notas.md", initialHash, "drive-id-99");

        var action = vault.EvaluateFileSync(
            "Trabajo/TCServi-Notas.md",
            new FileHash("222222"),
            new FileHash("333333"));

        Assert.Equal(SyncAction.Conflict, action);
    }

    [Fact]
    public void EvaluateFileSync_DeletedLocallyAndUnchangedRemotely_ShouldDeleteRemote()
    {
        var vault = new Vault(Guid.NewGuid(), "UOC-Notes", "C:/Notes", "folder-1");
        var initialHash = new FileHash("abc999");
        vault.MarkAsSynced("Daily/2026-10-01.md", initialHash, "drive-id-12");

        var action = vault.EvaluateFileSync("daily/2026-10-01.md", null, initialHash);

        Assert.Equal(SyncAction.DeleteRemote, action);
    }
}
