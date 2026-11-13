using System;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

/// <summary>FDW-5296: what a Graph data set reads, and what it only intends to read, is read from the settings its graph already holds.</summary>
public class GraphDataSetReadsTests
{
    private static GraphDataSetComponentConfiguration Component(
        string name, string type, string? dataSet = null, string? store = null, string? path = null, string? container = null) => new()
    {
        Name = name, ComponentType = type, DataSetName = dataSet, DataStoreName = store, PathName = path, ContainerName = container,
    };

    private static GraphDataSetConfiguration Graph(params GraphDataSetComponentConfiguration[] components) => new() { Name = "Revenue", Components = [.. components] };

    private static string[] Described(GraphDataSetConfiguration graph) =>
        [.. GraphDataSetReads.Of(graph).Select(read => $"{(read.IsIntended ? "intended " : string.Empty)}{read.Kind} {read.Name} [{string.Join("/", read.Parents)}]")];

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW5296")]
    public void ADataSetSourceReadsItsDataSet() =>
        Described(Graph(Component("src", "DataSetSource", dataSet: "Orders"))).ShouldBe(["DataSet Orders []"]);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW5296")]
    public void AContainerSourceReadsTheContainerUnderItsStoreAndPath() =>
        Described(Graph(Component("file", "ContainerSource", store: "Landing", path: "inbound", container: "orders.csv"), Component("plain", "ContainerSource", store: "Warehouse", container: "Sales")))
            .ShouldBe(["Container orders.csv [Landing/inbound]", "Container Sales [Warehouse]"]);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW5296")]
    public void AComponentThatNamesAStoreAndNoContainerSelectsFromTheStore() =>
        Described(Graph(Component("sql", "SqlSource", store: "Warehouse"))).ShouldBe(["DataStore Warehouse []"]);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW5296")]
    public void ATransformationThatNamesNothingReadsNothing() =>
        Described(Graph(Component("keep", "Filter"))).ShouldBeEmpty();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW5296")]
    public void AStageReadsTheDataSetAndTheContainerOfItsLookupsAndTheDataSetsOfItsComputeTables()
    {
        var lookup = Component("look", "Lookup");
        lookup.Stages =
        [
            new GraphDataSetStageConfiguration
            {
                Lookups =
                [
                    new GraphDataSetStageLookupConfiguration { LookupDataSet = "Rates" },
                    new GraphDataSetStageLookupConfiguration { LookupDataSet = string.Empty, LookupDataStoreName = "Warehouse", LookupDataStorePath = "ref", LookupContainerName = "Regions" },
                ],
                ComputeTables =
                [
                    new GraphDataSetStageComputeTableConfiguration { TableName = "t1", FromKind = "DataSet", FromName = "Shares" },
                    new GraphDataSetStageComputeTableConfiguration { TableName = "t2", FromKind = "Stage" },
                    new GraphDataSetStageComputeTableConfiguration { TableName = "t3", FromKind = "Output", FromName = "Earlier" },
                ],
            },
        ];

        Described(Graph(lookup)).ShouldBe(["DataSet Rates []", "Container Regions [Warehouse/ref]", "DataSet Shares []"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW5296")]
    public void ASourceReadTwiceIsListedOnceWithoutRegardToCase() =>
        Described(Graph(Component("a", "DataSetSource", dataSet: "Orders"), Component("b", "DataSetSource", dataSet: "ORDERS"))).ShouldBe(["DataSet Orders []"]);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW5296")]
    public void AnIntentIsListedAfterTheReadsMarkedIntendedInItsOrdinalOrderAndNamesWhatItsKindSays()
    {
        var graph = Graph();
        var id = Guid.NewGuid();
        graph.Intents =
        [
            new GraphDataSetIntentConfiguration { IntentKind = "DataSet", SourceDataSetId = id, SourceDataSetName = "Prices", Ordinal = 2 },
            new GraphDataSetIntentConfiguration { IntentKind = "DataStore", DataStoreName = "ExternalCrm", Ordinal = 1 },
        ];

        Described(graph).ShouldBe(["intended DataStore ExternalCrm []", "intended DataSet Prices []"]);
        GraphDataSetReads.Of(graph)[^1].DataSetId.ShouldBe(id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW5296")]
    public void AnIntentForWhatTheGraphAlreadyReadsAddsNothingAndAKindTheCollectionDoesNotHoldNamesNothing()
    {
        var graph = Graph(Component("src", "DataSetSource", dataSet: "Orders"));
        graph.Intents =
        [
            new GraphDataSetIntentConfiguration { IntentKind = "DataSet", SourceDataSetId = Guid.NewGuid(), SourceDataSetName = "orders", Ordinal = 0 },
            new GraphDataSetIntentConfiguration { IntentKind = "Container", DataStoreName = "Warehouse", Ordinal = 1 },
        ];

        Described(graph).ShouldBe(["DataSet Orders []"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW5296")]
    public void AnAbstractGraphWithNoIntentsReadsNothing() => GraphDataSetReads.Of(Graph()).ShouldBeEmpty();
}
