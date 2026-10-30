using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;
using static Fdw.Services.Etl.Tests.Pipelines.Loads.LoadTestDoubles;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for what a run that writes files claims (FDW-3193): two runs that write the same file take turns, two that write different files of one container
/// do not, and a run that writes files split by a field claims the whole container, since the files are known only when the rows arrive.
/// </summary>
public sealed class PipelineRunClaimsFilesTests
{
    private static readonly PipelineRunLimitValues Limits = new(8, 8);
    private readonly Guid _exports = Guid.NewGuid();

    private static PipelineComponentConfiguration Destination(string? kind, string? physicalName = null, string? splitField = null) => new()
    {
        Name = "players",
        ComponentType = "ContainerDestination",
        DataStoreName = "Exports",
        PathName = "exports",
        ContainerName = "Players",
        LoadKind = "Insert",
        TruncateFirst = false,
        TargetKind = kind,
        PhysicalName = physicalName,
        SplitField = splitField,
    };

    private async Task<IReadOnlyList<string>> KeysOf(PipelineComponentConfiguration destination)
    {
        var dataStores = DataStoreProviderOf(("Exports", "exports", "Players", FileContainer("Players", _exports, "exports/Players.csv")));
        var graph = Graph([DataSetSource("src"), destination], [Link("src", "players")]);
        graph.MaxParallelRuns = 3;
        graph.OverflowBehavior = "Queue";
        var request = new PipelineExecutionRequest
        {
            ExecutionId = Guid.NewGuid(), PipelineId = Guid.NewGuid(), PipelineName = "Export", TriggerSource = "Test", RunRoles = ["DataWriter"],
        };

        var claim = await PipelineRunClaims.For(
            request, graph, new LoadTargetResolver(null, dataStores.Object), Limits, NullLogger.Instance, TestContext.Current.CancellationToken);

        claim.IsSuccess.ShouldBeTrue(claim.CurrentMessage);
        claim.Value!.Alone.ShouldBeFalse();
        return claim.Value.WriteKeys.ToList();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TwoRunsThatWriteTheSameFileClaimTheSameKeyAndTwoThatWriteDifferentFilesDoNot()
    {
        var daily = await KeysOf(Destination("File", "daily.csv"));
        var again = await KeysOf(Destination("File", "daily.csv"));
        var weekly = await KeysOf(Destination("File", "weekly.csv"));

        daily.ShouldBe(again);
        daily.ShouldNotBe(weekly);
        daily.Single().ShouldEndWith("exports/daily.csv");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFilesDestinationClaimsTheWholeContainerAsADestinationWithNoKindDoes()
    {
        var plain = await KeysOf(Destination(null));
        var files = await KeysOf(Destination("Files", splitField: "Team"));

        files.ShouldBe(plain);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheUseOfAFileDestinationNamesTheFileItWrites()
    {
        var graph = Graph([DataSetSource("src"), Destination("File", "daily.csv")], [Link("src", "players")]);

        var uses = GraphPipelineUses.Of(graph, new Dictionary<string, object?>(), NullLogger.Instance);

        uses.IsSuccess.ShouldBeTrue(uses.CurrentMessage);
        uses.Value!.Single(use => use.Relation == PipelineUseRelations.Writes).PhysicalName.ShouldBe("daily.csv");
    }
}
