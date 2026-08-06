using System;
using System.Collections.Generic;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Workspace.Helpers;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Tests for <see cref="LedgerAssemblyIndex"/> — the type-to-assembly map the reference repair reads.
/// </summary>
public sealed class LedgerAssemblyIndexTests
{
    private static ChangeLedgerEntry Entry(params SymbolChange[] changes) =>
        new(1, "MoveTypeToProject", "moved", Array.Empty<LedgerFileChange>(), changes, Array.Empty<PathChange>());

    private static SymbolChange Moved(string fqn, string oldAssembly, string newAssembly) =>
        new(fqn, fqn, SymbolChangeTypes.Moved.Name, "NamedType", null, null, oldAssembly, newAssembly, null);

    private static SymbolChange Renamed(string oldFqn, string newFqn, string assembly) =>
        new(oldFqn, newFqn, SymbolChangeTypes.Renamed.Name, "NamedType", null, null, assembly, assembly, null);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ResolvesByFullyQualifiedName()
    {
        var index = new LedgerAssemblyIndex(new List<ChangeLedgerEntry>
        {
            Entry(Moved("Fdw.Data.MsSql.BinaryType", "Fdw.Services.Connections.MsSql", "Fdw.Data.Types.Databases")),
        });

        var lookup = index.Resolve("Fdw.Data.MsSql.BinaryType");

        lookup.IsResolved.ShouldBeTrue();
        lookup.Change.ShouldNotBeNull().NewAssembly.ShouldBe("Fdw.Data.Types.Databases");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ResolvesBySimpleNameBecauseTheCompilerOftenNamesOnlyTheType()
    {
        var index = new LedgerAssemblyIndex(new List<ChangeLedgerEntry>
        {
            Entry(Moved("Fdw.Data.MsSql.BinaryType", "Fdw.Services.Connections.MsSql", "Fdw.Data.Types.Databases")),
        });

        var lookup = index.Resolve("BinaryType");

        lookup.IsResolved.ShouldBeTrue();
        lookup.Change.ShouldNotBeNull().NewAssembly.ShouldBe("Fdw.Data.Types.Databases");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AmbiguousSimpleNameIsReportedNotGuessed()
    {
        var index = new LedgerAssemblyIndex(new List<ChangeLedgerEntry>
        {
            Entry(
                Moved("Fdw.A.Widget", "Src", "Fdw.Target.One"),
                Moved("Fdw.B.Widget", "Src", "Fdw.Target.Two")),
        });

        var lookup = index.Resolve("Widget");

        lookup.IsResolved.ShouldBeFalse();
        lookup.Reason.ShouldNotBeNull();
        lookup.Reason!.ShouldContain("ambiguous");
        lookup.Reason.ShouldContain("Fdw.Target.One");
        lookup.Reason.ShouldContain("Fdw.Target.Two");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void RenamesAreNotIndexedBecauseTheyAreGenuineConsumerBreaks()
    {
        var index = new LedgerAssemblyIndex(new List<ChangeLedgerEntry>
        {
            Entry(Renamed("Fdw.Old.Widget", "Fdw.New.Widget", "Fdw.Same")),
        });

        index.Count.ShouldBe(0);
        index.Resolve("Fdw.New.Widget").IsResolved.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MovesThatStayInTheSameAssemblyAreNotIndexed()
    {
        // A within-project file move needs no reference change, so it is not a repair candidate.
        var index = new LedgerAssemblyIndex(new List<ChangeLedgerEntry>
        {
            Entry(Moved("Fdw.Same.Widget", "Fdw.Same", "Fdw.Same")),
        });

        index.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void UnknownNameIsReportedWithAReason()
    {
        var index = new LedgerAssemblyIndex(Array.Empty<ChangeLedgerEntry>());

        var lookup = index.Resolve("Fdw.Never.Moved");

        lookup.IsResolved.ShouldBeFalse();
        lookup.Reason.ShouldNotBeNull();
        lookup.Reason!.ShouldContain("does not appear in the change ledger");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EmptyNameIsRejected()
    {
        var index = new LedgerAssemblyIndex(Array.Empty<ChangeLedgerEntry>());

        index.Resolve(string.Empty).IsResolved.ShouldBeFalse();
        index.Resolve("   ").IsResolved.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NullEntriesThrow()
    {
        // Cast disambiguates the two overloads; a real caller always passes a typed list.
        Should.Throw<ArgumentNullException>(() => new LedgerAssemblyIndex((IReadOnlyList<ChangeLedgerEntry>)null!));
        Should.Throw<ArgumentNullException>(() => new LedgerAssemblyIndex((IReadOnlyList<SymbolChange>)null!));
    }
}
