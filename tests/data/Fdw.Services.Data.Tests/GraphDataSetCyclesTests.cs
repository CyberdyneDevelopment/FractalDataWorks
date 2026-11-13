using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>FDW-5290: a cycle through the data sets a data set reads is found, with its path, and the depth of nesting is answered; each name is read once.</summary>
public sealed class GraphDataSetCyclesTests
{
    private static Task<IGenericResult<GraphDataSetCycleResult>> Find(
        string start, string[] startReads, Dictionary<string, string[]> stored, List<string[]>? batches = null) =>
        GraphDataSetCycles.Find(
            start,
            startReads,
            (names, _) =>
            {
                batches?.Add([.. names]);
                IReadOnlyDictionary<string, IReadOnlyList<string>> answer = names
                    .Where(stored.ContainsKey)
                    .ToDictionary(name => name, name => (IReadOnlyList<string>)stored[name], StringComparer.OrdinalIgnoreCase);
                return Task.FromResult<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>>(
                    GenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>.Success(answer));
            },
            TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task AStartThatReadsNothingHasNoCycleAndDepthZero()
    {
        var found = await Find("A", [], []);

        found.Value!.HasCycle.ShouldBeFalse();
        found.Value.Depth.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task TheDepthIsTheLongestChainBelowTheStart()
    {
        var found = await Find(
            "A", ["B", "D"],
            new Dictionary<string, string[]> { ["B"] = ["C"], ["C"] = ["E"], ["D"] = ["E"], ["E"] = [] });

        found.Value!.HasCycle.ShouldBeFalse();
        found.Value.Depth.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task AStartThatReadsItselfIsACycle()
    {
        var found = await Find("A", ["A"], []);

        found.Value!.Cycle.ShouldBe(new[] { "A", "A" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task ACycleBackToTheStartIsNamedAroundTheWholeLoop()
    {
        var found = await Find(
            "A", ["B"],
            new Dictionary<string, string[]> { ["B"] = ["C"], ["C"] = ["A"] });

        found.Value!.Cycle.ShouldBe(new[] { "A", "B", "C", "A" });
        found.Value.Depth.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task ACycleBelowTheStartThatDoesNotIncludeItIsStillFound()
    {
        var found = await Find(
            "A", ["B"],
            new Dictionary<string, string[]> { ["B"] = ["C"], ["C"] = ["B"] });

        found.Value!.Cycle.ShouldBe(new[] { "B", "C", "B" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task TheDraftsOwnReadsReplaceWhatIsStoredUnderItsName()
    {
        // Stored, A reads nothing; the draft of A reads B and B reads A.
        var found = await Find("A", ["B"], new Dictionary<string, string[]> { ["A"] = [], ["B"] = ["A"] });

        found.Value!.Cycle.ShouldBe(new[] { "A", "B", "A" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task NamesAreComparedWithoutRegardToCase()
    {
        var found = await Find("A", ["b"], new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["B"] = ["a"] });

        found.Value!.HasCycle.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task EachNameIsReadOnceInOneBatchPerLevel()
    {
        var batches = new List<string[]>();

        await Find(
            "A", ["B", "C"],
            new Dictionary<string, string[]> { ["B"] = ["D"], ["C"] = ["D"], ["D"] = [] },
            batches);

        batches.Count.ShouldBe(2);
        batches[0].ShouldBe(["B", "C"], ignoreOrder: true);
        batches[1].ShouldBe(new[] { "D" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task AFailedReadIsTheAnswer()
    {
        var found = await GraphDataSetCycles.Find(
            "A",
            ["B"],
            (_, _) => Task.FromResult<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>>(
                GenericResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>.Failure(
                    Fdw.Services.Data.Results.DataServiceResultCodes.ByName("GraphDataSetNotReadable"),
                    Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance,
                    ResultDetails.Create("dataSet", "B"))),
            TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
    }
}
