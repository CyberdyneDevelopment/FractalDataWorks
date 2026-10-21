using System.Collections.Generic;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// What a saved pipeline reaches (FDW-1161, FDW-1891): triggering it, scheduling it and judging the roles for it all read
/// <see cref="PipelineReach.Of"/> from the pipeline read whole, so a graph's run is judged on what its components and stages use.
/// </summary>
public sealed class PipelineReachTests
{
    private static readonly IReadOnlyDictionary<string, object?> NoValues = new Dictionary<string, object?>();

    private static PipelineReach ReachOf(GraphPipelineConfiguration graph) => ReachOf(graph, NoValues);

    private static PipelineReach ReachOf(GraphPipelineConfiguration graph, IReadOnlyDictionary<string, object?> values)
    {
        var found = PipelineReach.Of(graph, values, NullLogger.Instance);
        found.IsSuccess.ShouldBeTrue(found.CurrentMessage);
        return found.Value.ShouldNotBeNull();
    }

    private static PipelineComponentConfiguration Component(string type, string? dataSet = null, string? store = null, string? path = null, string? container = null) => new()
    {
        Name = type, ComponentType = type, DataSetName = dataSet, DataStoreName = store, PathName = path, ContainerName = container,
    };

    private static GraphPipelineConfiguration Graph(params PipelineComponentConfiguration[] components) => new()
    {
        Name = "Nightly",
        Components = [.. components],
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AGraphReadsItsSourceDataSetAndWritesItsDestinationDataSet()
    {
        var reach = ReachOf(Graph(Component("DataSetSource", dataSet: "Orders"), Component("DataSetDestination", dataSet: "OrdersCopy")));

        reach.PipelineName.ShouldBe("Nightly");
        reach.DataSets.ShouldBe([new ReachedDataSet("Orders", false), new ReachedDataSet("OrdersCopy", true)]);
        reach.Containers.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AGraphWithSeveralSourcesReachesEveryOne()
    {
        var reach = ReachOf(Graph(
            Component("DataSetSource", dataSet: "Orders"),
            Component("DataSetSource", dataSet: "Customers"),
            Component("DataSetDestination", dataSet: "Joined")));

        reach.DataSets.ShouldBe([new ReachedDataSet("Orders", false), new ReachedDataSet("Customers", false), new ReachedDataSet("Joined", true)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AGraphOverDataStoreContainersReachesTheirThreePartsAsAReadAndAWrite()
    {
        var reach = ReachOf(Graph(
            Component("ContainerSource", store: "Feeds", path: "orders/v1", container: "orders"),
            Component("ContainerDestination", store: "Warehouse", path: "orders/v1", container: "orders-clean")));

        reach.Containers.ShouldBe([
            new ReachedContainer("Feeds", "orders/v1", "orders", false),
            new ReachedContainer("Warehouse", "orders/v1", "orders-clean", true)]);
        reach.DataSets.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AContainerThatNamesNoPathIsReachedWithAnEmptyPathSoTheAuthorityRefusesIt()
    {
        var reach = ReachOf(Graph(Component("ContainerSource", store: "Feeds", container: "orders")));

        reach.Containers.ShouldBe([new ReachedContainer("Feeds", string.Empty, "orders", false)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ALookupAndAComputeTableInsideAToolAreReachedWhereverTheStageSits()
    {
        var tool = Component("Compute");
        tool.Stages =
        [
            new PipelineTransformConfiguration
            {
                Name = "Enrich",
                Lookups = [new PipelineTransformLookupConfiguration { Name = "a", LookupDataSet = "Customers" }],
                ComputeTables = [new PipelineTransformComputeTableConfiguration { TableName = "t", FromKind = "DataSet", FromName = "Prices" }],
            },
        ];

        var reach = ReachOf(Graph(tool));

        reach.DataSets.ShouldBe([
            new ReachedDataSet("Customers", false),
            new ReachedDataSet("Prices", false)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ALookupOverADataStoreContainerReadsThatContainerAndReachesNoDataSet()
    {
        var tool = Component("Compute");
        tool.Stages =
        [
            new PipelineTransformConfiguration
            {
                Name = "Enrich",
                Lookups =
                [
                    new PipelineTransformLookupConfiguration
                    {
                        Name = "a", LookupDataStoreName = "Warehouse", LookupDataStorePath = "ref/v1", LookupContainerName = "customers",
                    },
                ],
            },
        ];

        var reach = ReachOf(Graph(tool));

        reach.Containers.ShouldBe([new ReachedContainer("Warehouse", "ref/v1", "customers", false)]);
        reach.DataSets.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ANameSeveralComponentsShareAppearsOnce()
    {
        var reach = ReachOf(Graph(
            Component("DataSetDestination", dataSet: "Staging"),
            Component("DataSetDestination", dataSet: "STAGING")));

        reach.DataSets.ShouldBe([new ReachedDataSet("Staging", true)]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ABlankNameIsNotReachedAndAPipelineReadAsARecordAloneReachesNothing()
    {
        ReachOf(Graph(Component("DataSetSource", dataSet: " "))).DataSets.ShouldBeEmpty();

        var record = ReachOf(new GraphPipelineConfiguration { Name = "Nightly" });

        record.DataSets.ShouldBeEmpty();
        record.Containers.ShouldBeEmpty();
    }

    // FDW-1891: a source may name its data set by a parameter, and the run reaches the name it is given.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ADataSetNamedByAPlaceholderIsJudgedByTheValueTheRunIsGiven()
    {
        var graph = Graph(Component("DataSetSource", dataSet: "{{Which}}"), Component("DataSetDestination", dataSet: "Out"));

        var reach = ReachOf(graph, new Dictionary<string, object?> { ["Which"] = "BreweryFeed" });

        reach.DataSets.ShouldBe([new ReachedDataSet("BreweryFeed", false), new ReachedDataSet("Out", true)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ADataSetBoundToAParameterIsJudgedByTheValueTheRunIsGiven()
    {
        var source = Component("DataSetSource");
        source.SettingBindings = [new PipelineComponentSettingBindingConfiguration { SettingName = "dataSet", ParameterName = "Which" }];

        var reach = ReachOf(Graph(source), new Dictionary<string, object?> { ["Which"] = "BreweryRejects" });

        reach.DataSets.ShouldBe([new ReachedDataSet("BreweryRejects", false)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AParameterTheRunWasNotGivenRefusesTheReachNamingIt()
    {
        var graph = Graph(Component("DataSetSource", dataSet: "{{Which}}"));

        var found = PipelineReach.Of(graph, NoValues, NullLogger.Instance);

        found.IsSuccess.ShouldBeFalse();
        found.CurrentMessage.ShouldNotBeNull().ShouldContain("Which");
    }
}
