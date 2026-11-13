using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Results;
using Fdw.Services.Etl.Graphs;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Graphs.Tests;

/// <summary>FDW-5454: a draft that is not stored is read with a cap on the rows each source gives, at its stated end or at a chosen component's output, and every refusal is its own code.</summary>
public sealed class GraphDataSetPreviewerTests
{
    private static GraphDataSetPreviewer Previewer(int rowsPerSource = 2, int maxRows = 100)
    {
        var read = new GraphDataSetReadOptions(maxRows, 5, 2);
        return new GraphDataSetPreviewer(
            new GraphDataSetRunner(read, NullLogger<GraphDataSetRunner>.Instance), read, new GraphDataSetPreviewOptions(rowsPerSource), NullLogger<GraphDataSetPreviewer>.Instance);
    }

    private static GraphDataSetConfiguration Draft(string? component = "src", string? output = "output")
    {
        var draft = new GraphDataSetConfiguration { Name = "Weather", Implementation = "Graph", FieldsOutComponent = component, FieldsOutOutput = output };
        draft.Components.Add(new GraphDataSetComponentConfiguration { Name = "src", ComponentType = "DataSetSource", DataSetName = "Readings", BatchSize = 2 });
        return draft;
    }

    private static IDataGateway ServingRows(int count)
    {
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows =
            [.. Enumerable.Range(0, count).Select(i => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?> { ["Id"] = i })];
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(GenericResult<IRecordSource<DataRecord>>.Success(new RowListRecordSource(RowListRecordSource.SchemaOfRows(rows), rows))));
        return gateway.Object;
    }

    private static Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>> Preview(
        GraphDataSetPreviewer previewer, GraphDataSetConfiguration draft, GraphPreviewRequest request, int rows = 10) =>
        previewer.Preview(ServingRows(rows), draft, request, TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5454")]
    public async Task ACapTheCallerStatesLimitsTheRowsEachSourceGivesAndNothingIsRefused()
    {
        var result = await Preview(Previewer(rowsPerSource: 5), Draft(), new GraphPreviewRequest(3, null, null));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5454")]
    public async Task ACapTheCallerLeavesOutIsTheConfiguredNumber()
    {
        var result = await Preview(Previewer(rowsPerSource: 4), Draft(), new GraphPreviewRequest(null, null, null));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Count.ShouldBe(4);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5454")]
    public async Task ACapBelowOneAnswersItsOwnCodeAndReadsNothing(int cap)
    {
        var result = await Preview(Previewer(), Draft(), new GraphPreviewRequest(cap, null, null));

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetPreviewCapInvalidCode>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5454")]
    public async Task AConfiguredCapBelowOneIsRefusedTheSameWay()
    {
        var result = await Preview(Previewer(rowsPerSource: 0), Draft(), new GraphPreviewRequest(null, null, null));

        result.Code.ShouldBeOfType<GraphDataSetPreviewCapInvalidCode>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5454")]
    public async Task ADraftWithNoStatedEndAndNoChosenOutputIsNotBound()
    {
        var result = await Preview(Previewer(), Draft(null, null), new GraphPreviewRequest(3, null, null));

        result.Code.ShouldBeOfType<GraphDataSetNotBoundCode>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5454")]
    public async Task ADraftWithNoStatedEndCanStillBeReadAtAChosenOutput()
    {
        var result = await Preview(Previewer(), Draft(null, null), new GraphPreviewRequest(3, "src", "output"));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Count.ShouldBe(3);
    }

    [Theory]
    [InlineData("nothere", "output", "no component of that name")]
    [InlineData("src", "nope", "no output of that name")]
    [InlineData("src", null, "stated together")]
    [InlineData(null, "output", "stated together")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5454")]
    public async Task AChosenEndTheDraftDoesNotHaveAnswersItsOwnCodeAndReadsNothing(string? component, string? output, string reason)
    {
        var result = await Preview(Previewer(), Draft(), new GraphPreviewRequest(3, component, output));

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetPreviewEndNotFoundCode>();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(reason);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5454")]
    public async Task ThePreviewIsHeldToTheRowLimitOfAReadAndAnswersTheNamedFailure()
    {
        var result = await Preview(Previewer(maxRows: 2), Draft(), new GraphPreviewRequest(5, null, null));

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetRowLimitExceededCode>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5454")]
    public void KeepingUpToAComponentLeavesOutWhatDoesNotFeedItAndDropsTheOutputsItLosesNoOtherLine()
    {
        var graph = new GraphPipelineConfiguration { Name = "G", LinkCapacity = 2 };
        foreach (var name in new[] { "s1", "s2", "c", "x", "y" })
        {
            graph.Components.Add(new PipelineComponentConfiguration { Name = name, ComponentType = "DataSetSource" });
        }

        graph.Links.Add(new PipelineLinkConfiguration { FromComponent = "s1", FromOutput = "output", ToComponent = "c", ToInput = "input" });
        graph.Links.Add(new PipelineLinkConfiguration { FromComponent = "s2", FromOutput = "output", ToComponent = "y", ToInput = "input" });
        graph.Links.Add(new PipelineLinkConfiguration { FromComponent = "c", FromOutput = "a", ToComponent = "x", ToInput = "input" });
        graph.Links.Add(new PipelineLinkConfiguration { FromComponent = "c", FromOutput = "b", ToComponent = "y", ToInput = "input" });

        var kept = GraphDataSetPipelines.UpTo(graph, new GraphEnd("c", "a"));

        kept.Components.Select(component => component.Name).ShouldBe(["s1", "c"], ignoreOrder: true);
        kept.Links.Select(link => $"{link.FromComponent}->{link.ToComponent}").ShouldBe(["s1->c"]);
        kept.DroppedOutputs.Select(output => $"{output.ComponentName}/{output.OutputName}").ShouldBe(["c/b"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5454")]
    public void AnOutputThatKeepsALineToAKeptComponentIsNotDroppedAndTheEndsOwnOutputIsNeverDropped()
    {
        var graph = new GraphPipelineConfiguration { Name = "G", LinkCapacity = 2 };
        foreach (var name in new[] { "s", "m", "later" })
        {
            graph.Components.Add(new PipelineComponentConfiguration { Name = name, ComponentType = "DataSetSource" });
        }

        graph.Links.Add(new PipelineLinkConfiguration { FromComponent = "s", FromOutput = "output", ToComponent = "m", ToInput = "input" });
        graph.Links.Add(new PipelineLinkConfiguration { FromComponent = "s", FromOutput = "output", ToComponent = "later", ToInput = "input" });
        graph.Links.Add(new PipelineLinkConfiguration { FromComponent = "m", FromOutput = "output", ToComponent = "later", ToInput = "input" });

        var kept = GraphDataSetPipelines.UpTo(graph, new GraphEnd("m", "output"));

        kept.Components.Select(component => component.Name).ShouldBe(["s", "m"], ignoreOrder: true);
        kept.DroppedOutputs.ShouldBeEmpty();
    }
}
