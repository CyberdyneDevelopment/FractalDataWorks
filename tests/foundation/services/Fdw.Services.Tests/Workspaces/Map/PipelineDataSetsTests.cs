using System.Linq;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Workspaces.Endpoints;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces.Map;

/// <summary>FDW-1683: the data sets a graph pipeline reads and writes are named from the settings of its sources and destinations.</summary>
public class PipelineDataSetsTests
{
    private static GraphPipelineConfiguration Graph() => new()
    {
        Name = "Load",
        Components =
        [
            new PipelineComponentConfiguration { Name = "a", ComponentType = "DataSetSource", DataSetName = "Orders" },
            new PipelineComponentConfiguration { Name = "b", ComponentType = "DataSetSource", DataSetName = "Customers" },
            new PipelineComponentConfiguration { Name = "c", ComponentType = "DataSetDestination", DataSetName = "Report" },
        ],
    };

    [Fact]
    public void AGraphPipelineNamesEveryDataSetItsSourcesReadAndItsDestinationsWrite()
    {
        PipelineDataSets.Reads(Graph()).ShouldBe(["Orders", "Customers"]);
        PipelineDataSets.Writes(Graph()).ShouldBe(["Report"]);
    }

    [Fact]
    public void AGraphPipelineReadAsARecordAloneNamesNothing()
    {
        PipelineDataSets.Reads(new GraphPipelineConfiguration { Name = "Load" }).ShouldBeEmpty();
    }

    [Fact]
    public void AGraphPipelineNamesTheContainersItReadsAndWritesDirectlyByStorePathAndName()
    {
        var graph = new GraphPipelineConfiguration
        {
            Name = "Copy",
            Components =
            [
                new PipelineComponentConfiguration { Name = "a", ComponentType = "ContainerSource", ContainerName = "Raw", DataStoreName = "Lake", PathName = "landing" },
                new PipelineComponentConfiguration { Name = "b", ComponentType = "ContainerDestination", ContainerName = "Clean", DataStoreName = "Warehouse" },
            ],
        };

        PipelineContainers.Reads(graph).ShouldBe([new PipelineContainerRef("Lake", "landing", "Raw")]);
        PipelineContainers.Writes(graph).ShouldBe([new PipelineContainerRef("Warehouse", null, "Clean")]);
    }

    [Fact]
    public void AContainerNamedWithoutADataStoreIsNotNamed()
    {
        var graph = new GraphPipelineConfiguration
        {
            Name = "Copy",
            Components = [new PipelineComponentConfiguration { Name = "a", ComponentType = "ContainerSource", ContainerName = "Raw" }],
        };

        PipelineContainers.Reads(graph).ShouldBeEmpty();
    }
}
