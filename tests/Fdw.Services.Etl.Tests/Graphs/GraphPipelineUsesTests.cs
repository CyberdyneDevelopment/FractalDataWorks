using System.Linq;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-1683: what a graph pipeline uses is read from the settings it already holds: the data sets and containers its sources read and
/// its destinations write, the pipelines its actions start, the data stores its SQL actions run against.
/// </summary>
public sealed class GraphPipelineUsesTests
{
    private static PipelineComponentConfiguration Component(string name, string type, string? dataSet = null, string? store = null, string? path = null, string? container = null) => new()
    {
        Name = name, ComponentType = type, DataSetName = dataSet, DataStoreName = store, PathName = path, ContainerName = container,
    };

    private static GraphPipelineConfiguration Graph(params PipelineComponentConfiguration[] components) => new() { Name = "Load", Components = [.. components] };

    private static PipelineComponentConfiguration[] Flow(params PipelineComponentConfiguration[] components) => components;

    private static string[] Described(GraphPipelineConfiguration graph) =>
        [.. GraphPipelineUses.Of(graph).Select(use => $"{use.Relation} {use.Kind} {use.Name} [{string.Join("/", use.Parents)}]")];

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ASourceReadsItsDataSetAndADestinationWritesItsOwn()
    {
        var graph = Graph(Flow(Component("src", "DataSetSource", dataSet: "Breweries"), Component("keep", "DataSetDestination", dataSet: "BreweryRejects")));

        Described(graph).ShouldBe(["reads DataSet Breweries []", "writes DataSet BreweryRejects []"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AContainerSourceReadsTheContainerUnderItsStoreAndPath()
    {
        var graph = Graph(Flow(
            Component("file", "ContainerSource", store: "Landing", path: "inbound", container: "orders.csv"),
            Component("out", "ContainerDestination", store: "Landing", container: "orders-clean.csv")));

        Described(graph).ShouldBe(["reads Container orders.csv [Landing/inbound]", "writes Container orders-clean.csv [Landing]"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AnActionThatStartsAPipelineCallsItAndASqlActionUsesItsStore()
    {
        var graph = Graph(
            new PipelineComponentConfiguration { Name = "child", ComponentType = "ExecutePipeline", TargetPipeline = "Cleanup" },
            new PipelineComponentConfiguration { Name = "tidy", ComponentType = "ExecuteSql", Stage = 1, DataStoreName = "SalesDb" });

        Described(graph).ShouldBe(["calls Pipeline Cleanup []", "uses DataStore SalesDb []"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ASqlSourceReadsItsStoreAndALookupReadsItsDataSet()
    {
        var graph = Graph(Flow(Component("sql", "SqlSource", store: "SalesDb"), Component("lookup", "Lookup", dataSet: "Regions")));

        Described(graph).ShouldBe(["reads DataStore SalesDb []", "reads DataSet Regions []"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheSameUseIsListedOnceWhateverTheCase()
    {
        var graph = Graph(Flow(Component("a", "DataSetSource", dataSet: "Breweries"), Component("b", "DataSetSource", dataSet: "BREWERIES")));

        Described(graph).ShouldBe(["reads DataSet Breweries []"]);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void APipelineReadAsARecordAloneUsesNothing()
    {
        GraphPipelineUses.Of(new GraphPipelineConfiguration { Name = "Load" }).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DestinationsAreToldFromSourcesByTheCatalogueShapeOfTheirType()
    {
        // Why this shape: the catalogue says a destination has inputs and no outputs, so a read or a write is that, not the end of a name.
        var graph = Graph(Flow(Component("keep", "DataSetDestination", dataSet: "Out"), Component("get", "DataSetSource", dataSet: "In")));

        Described(graph).ShouldBe(["writes DataSet Out []", "reads DataSet In []"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AComponentTypeTheCatalogueDoesNotKnowSaysNothingAboutReadsOrWrites()
    {
        var graph = Graph(Flow(Component("odd", "NoSuchComponent", dataSet: "Mystery")));

        Described(graph).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AToolThatNamesATransformationByIdIsListedWithItsNameWhenTheNameIsKnown()
    {
        var known = Guid.NewGuid();
        var unknown = Guid.NewGuid();
        var graph = Graph(Flow(
            new PipelineComponentConfiguration
            {
                Name = "a", ComponentType = "Compute", Stages = [new Fdw.Services.Etl.Transforms.PipelineTransformConfiguration { Name = "s", ComputeTransformationId = known }],
            },
            new PipelineComponentConfiguration
            {
                Name = "b", ComponentType = "Compute", Stages = [new Fdw.Services.Etl.Transforms.PipelineTransformConfiguration { Name = "t", ComputeTransformationId = unknown }],
            }));

        var uses = GraphPipelineUses.Of(graph, new Dictionary<Guid, string> { [known] = "Margin" });

        uses.Select(use => $"{use.Relation} {use.Kind} {use.Name}").ShouldBe(["uses Transformation Margin"]);
    }

    private static GraphPipelineConfiguration GraphWithStage(Fdw.Services.Etl.Transforms.PipelineTransformConfiguration stage) =>
        Graph(Flow(new PipelineComponentConfiguration { Name = "tool", ComponentType = "Compute", Stages = [stage] }));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ALookupStageReadsTheDataSetItNamesOnce()
    {
        var graph = GraphWithStage(new Fdw.Services.Etl.Transforms.PipelineTransformConfiguration
        {
            Name = "Enrich",
            Lookups =
            [
                new Fdw.Services.Etl.Transforms.PipelineTransformLookupConfiguration { Name = "a", LookupDataSet = "Customers" },
                new Fdw.Services.Etl.Transforms.PipelineTransformLookupConfiguration { Name = "b", LookupDataSet = "Customers" },
            ],
        });

        Described(graph).ShouldBe(["reads DataSet Customers []"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ALookupStageOverAContainerReadsThatContainerUnderItsStoreAndPath()
    {
        var graph = GraphWithStage(new Fdw.Services.Etl.Transforms.PipelineTransformConfiguration
        {
            Name = "Enrich",
            Lookups =
            [
                new Fdw.Services.Etl.Transforms.PipelineTransformLookupConfiguration
                {
                    Name = "a", LookupDataStoreName = "Warehouse", LookupDataStorePath = "ref/v1", LookupContainerName = "customers",
                },
            ],
        });

        Described(graph).ShouldBe(["reads Container customers [Warehouse/ref/v1]"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AComputeTableBoundToADataSetReadsItAndOneBoundToAStageOrOutputReadsNothing()
    {
        var graph = GraphWithStage(new Fdw.Services.Etl.Transforms.PipelineTransformConfiguration
        {
            Name = "Margin",
            ComputeTables =
            [
                new Fdw.Services.Etl.Transforms.PipelineTransformComputeTableConfiguration { TableName = "t1", FromKind = "DataSet", FromName = "Prices" },
                new Fdw.Services.Etl.Transforms.PipelineTransformComputeTableConfiguration { TableName = "t2", FromKind = "Stage" },
                new Fdw.Services.Etl.Transforms.PipelineTransformComputeTableConfiguration { TableName = "t3", FromKind = "Output", FromName = "Cleaned" },
            ],
        });

        Described(graph).ShouldBe(["reads DataSet Prices []"]);
    }
}
