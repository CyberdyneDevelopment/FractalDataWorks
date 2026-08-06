using System.Linq;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Abstractions.Results;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Unit tests for <see cref="ChangeLedger"/>.
/// </summary>
public sealed class ChangeLedgerTests
{
    private static FileChange NewFileChange(string path = "/repo/Foo.cs") =>
        new(path, FileChangeTypes.Modified, "Foo") { TextChangeCount = 1 };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void RecordAssignsSequentialSequenceNumbers()
    {
        var ledger = new ChangeLedger();

        var first = ledger.Record(
            "Rename", "summary 1", Array.Empty<FileChange>(), Array.Empty<SymbolChange>(), Array.Empty<PathChange>());
        var second = ledger.Record(
            "MoveToFile", "summary 2", Array.Empty<FileChange>(), Array.Empty<SymbolChange>(), Array.Empty<PathChange>());

        first.Sequence.ShouldBe(1);
        second.Sequence.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void EntriesReturnsSnapshotInSequenceOrder()
    {
        var ledger = new ChangeLedger();
        ledger.Record("A", "a", Array.Empty<FileChange>(), Array.Empty<SymbolChange>(), Array.Empty<PathChange>());
        ledger.Record("B", "b", Array.Empty<FileChange>(), Array.Empty<SymbolChange>(), Array.Empty<PathChange>());

        var entries = ledger.Entries;

        entries.Count.ShouldBe(2);
        entries[0].CommandName.ShouldBe("A");
        entries[1].CommandName.ShouldBe("B");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ClearRemovesAllEntriesAndResetsSequence()
    {
        var ledger = new ChangeLedger();
        ledger.Record("A", "a", Array.Empty<FileChange>(), Array.Empty<SymbolChange>(), Array.Empty<PathChange>());

        ledger.Clear();

        ledger.Entries.Count.ShouldBe(0);
        var next = ledger.Record("B", "b", Array.Empty<FileChange>(), Array.Empty<SymbolChange>(), Array.Empty<PathChange>());
        next.Sequence.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void RecordMapsFileChangeToLedgerFileChangePreservingChangeTypeName()
    {
        var ledger = new ChangeLedger();
        var fileChange = NewFileChange();

        var entry = ledger.Record(
            "Rename", "summary", new[] { fileChange }, Array.Empty<SymbolChange>(), Array.Empty<PathChange>());

        entry.FileChanges.Count.ShouldBe(1);
        entry.FileChanges[0].FilePath.ShouldBe(fileChange.FilePath);
        entry.FileChanges[0].ChangeType.ShouldBe(FileChangeTypes.Modified.Name);
        entry.FileChanges[0].ProjectName.ShouldBe(fileChange.ProjectName);
        entry.FileChanges[0].TextChangeCount.ShouldBe(fileChange.TextChangeCount);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ConcurrentRecordCallsProduceDistinctSequenceNumbers()
    {
        var ledger = new ChangeLedger();

        var tasks = Enumerable.Range(0, 50)
            .Select(i => Task.Run(() => ledger.Record(
                $"Cmd{i}", "summary", Array.Empty<FileChange>(), Array.Empty<SymbolChange>(), Array.Empty<PathChange>())))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        results.Select(r => r.Sequence).Distinct().Count().ShouldBe(50);
        ledger.Entries.Count.ShouldBe(50);
    }
}
