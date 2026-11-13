using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>FDW-5299: a Graph data set read through other data sets is refused on a cycle and past the stated depth, and answered otherwise, whatever the kinds it nests.</summary>
public sealed class GraphDataSetNestingTests
{
    private static GraphDataSetConfiguration Graph(string name, params string[] reads) => new()
    {
        Name = name,
        Components = [.. reads.Select((read, position) => new GraphDataSetComponentConfiguration { Name = $"src{position}", ComponentType = "DataSetSource", DataSetName = read })],
    };

    private static Task<IGenericResult> Check(GraphDataSetConfiguration start, FakeGraphDataSetSources sources, int maxDepth = 5) =>
        GraphDataSetNesting.Check(start, sources, maxDepth, NullLogger.Instance, TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task AGraphReadingNoDataSetNestsNothing()
    {
        var result = await Check(Graph("Top"), new FakeGraphDataSetSources());

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task AGraphOverAGraphOverASimpleDataSetIsTwoLevelsDeepAndIsAnswered()
    {
        var sources = new FakeGraphDataSetSources().WithDataSet("Mid", "Base").WithDataSet("Base");

        var result = await Check(Graph("Top", "Mid"), sources, maxDepth: 2);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task AGraphOverAVariationOverAGraphOverASimpleDataSetIsThreeLevelsDeepAndIsAnswered()
    {
        var sources = new FakeGraphDataSetSources().WithDataSet("ByDivision", "Mid").WithDataSet("Mid", "Base").WithDataSet("Base");

        var result = await Check(Graph("Top", "ByDivision"), sources, maxDepth: 3);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task ANestingDeeperThanTheStatedDepthAnswersTheNamedFailureWithTheDepthAndTheLimit()
    {
        var sources = new FakeGraphDataSetSources().WithDataSet("Mid", "Low").WithDataSet("Low", "Base").WithDataSet("Base");

        var result = await Check(Graph("Top", "Mid"), sources, maxDepth: 2);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetDepthExceededCode>();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Top");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("3");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("2");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task ACycleMadeAfterTheSaveIsRefusedWithTheChainAndNeverFollowed()
    {
        var sources = new FakeGraphDataSetSources().WithDataSet("A", "B").WithDataSet("B", "A");

        var result = await Check(Graph("Top", "A"), sources);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetCycleCode>();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("A -> B -> A");
        sources.DataSetsAsked.GroupBy(name => name, StringComparer.OrdinalIgnoreCase).ShouldAllBe(group => group.Count() == 1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task AGraphThatIsReadByAGraphItReadsIsACycleBackToTheStart()
    {
        var sources = new FakeGraphDataSetSources().WithDataSet("Mid", "Top");

        var result = await Check(Graph("Top", "Mid"), sources);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetCycleCode>();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Top -> Mid -> Top");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task ACycleThroughADataSetAStageLookupNamesIsFoundBecauseTheStageIsPartOfWhatTheGraphReads()
    {
        var top = Graph("Top", "Mid");
        top.Components[0].Stages =
        [
            new GraphDataSetStageConfiguration { Lookups = [new GraphDataSetStageLookupConfiguration { LookupDataSet = "Rates" }] },
        ];
        var sources = new FakeGraphDataSetSources().WithDataSet("Mid").WithDataSet("Rates", "Top");

        var result = await Check(top, sources);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetCycleCode>();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Top -> Rates -> Top");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task ADataSetAGraphOnlyIntendsToReadIsNotAnEdge()
    {
        var top = Graph("Top");
        top.Intents.Add(new GraphDataSetIntentConfiguration { IntentKind = "DataSet", SourceDataSetName = "Mid", Ordinal = 0 });
        var sources = new FakeGraphDataSetSources().WithDataSet("Mid", "Top");

        var result = await Check(top, sources);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        sources.DataSetsAsked.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task AFailedReadOfANestedDefinitionIsTheFailureAnsweredAndNotACycleOrAnAnswer()
    {
        var result = await Check(Graph("Top", "Mid"), new FakeGraphDataSetSources { Fails = true });

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldNotBeOfType<GraphDataSetCycleCode>();
        result.Code.ShouldNotBeOfType<GraphDataSetDepthExceededCode>();
    }
}
