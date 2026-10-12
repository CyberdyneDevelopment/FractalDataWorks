using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Workspace.Commands;
using Fdw.Roslyn.Commands.Workspace.Translators;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Unit tests for <see cref="GetChangeLedgerTranslator"/>.
/// </summary>
public sealed class GetChangeLedgerTranslatorTests
{
    private static Solution NewSolution() => new AdhocWorkspace().CurrentSolution;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TranslateReturnsLedgerNotAvailableWhenLedgerIsNull()
    {
        var translator = new GetChangeLedgerTranslator();
        var command = new GetChangeLedgerCommand { Ledger = null };

        var result = await translator.Translate(command, NewSolution(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code.Name.ShouldBe("LedgerNotAvailable");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TranslateReturnsZeroCountsWhenLedgerIsEmpty()
    {
        var translator = new GetChangeLedgerTranslator();
        var command = new GetChangeLedgerCommand { Ledger = new ChangeLedger() };

        var result = await translator.Translate(command, NewSolution(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var data = result.Value.ShouldNotBeNull().Data;
        data.TotalEntries.ShouldBe(0);
        data.RenameCount.ShouldBe(0);
        data.MoveCount.ShouldBe(0);
        data.AddedCount.ShouldBe(0);
        data.RemovedCount.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TranslateComputesCountsByChangeType()
    {
        var ledger = new ChangeLedger();
        var renamed = new SymbolChange("Old.A", "Old.B", SymbolChangeTypes.Renamed.Name, "Method", null, null, "Asm", "Asm", null);
        var moved = new SymbolChange("Old.C", "Old.C", SymbolChangeTypes.Moved.Name, "NamedType", "/a.cs", "/b.cs", "Asm", "Asm", "b.cs");
        var added = new SymbolChange("Old.D", "Old.E", SymbolChangeTypes.Added.Name, "Property", null, null, "Asm", "Asm", null);
        ledger.Record(
            "Rename", "summary", Array.Empty<FileChange>(), new[] { renamed, moved, added }, Array.Empty<PathChange>());

        var translator = new GetChangeLedgerTranslator();
        var command = new GetChangeLedgerCommand { Ledger = ledger };

        var result = await translator.Translate(command, NewSolution(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var data = result.Value.ShouldNotBeNull().Data;
        data.TotalEntries.ShouldBe(1);
        data.RenameCount.ShouldBe(1);
        data.MoveCount.ShouldBe(1);
        data.AddedCount.ShouldBe(1);
        data.RemovedCount.ShouldBe(0);
    }
}
