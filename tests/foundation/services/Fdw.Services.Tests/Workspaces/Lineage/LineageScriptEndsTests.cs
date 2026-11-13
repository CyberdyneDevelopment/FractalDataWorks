using System;
using System.Linq;
using Fdw.Services.Connections;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Workspaces.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces.Lineage;

/// <summary>
/// FDW-5457: a SQL step draws the containers its script reads and writes, from a parse of the script in its data store's language; a stored script
/// that cannot be read draws nothing and leaves the rest of the pipeline as it was.
/// </summary>
public class LineageScriptEndsTests
{
    private static readonly Guid PipelineId = Guid.CreateVersion7();

    private static IDataStoreImplementationConfiguration Store(string kind)
    {
        var store = new Mock<IDataStoreImplementationConfiguration>();
        store.SetupGet(s => s.Implementation).Returns(kind);
        store.SetupGet(s => s.Paths).Returns([new DataPathConfiguration { Name = "NflData", Containers = [new DataContainerConfiguration { Name = "Team" }] }]);
        return store.Object;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AScriptDrawsTheTablesItReadsAndTheTablesItWrites()
    {
        var ends = LineageScriptEnds.Of(
            PipelineId, new GraphScript("load", "NflDb", "INSERT INTO NflData.Game (Id) SELECT Id FROM NflData.Team"), Store("MsSql"), NullLogger.Instance);

        ends.ShouldBe(
        [
            (new PipelineContainerRef("NflDb", "NflData", "Team"), true),
            (new PipelineContainerRef("NflDb", "NflData", "Game"), false),
        ]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AStoredScriptThatDoesNotParseDrawsNothingAndDoesNotFail()
    {
        LineageScriptEnds.Of(PipelineId, new GraphScript("load", "NflDb", "INSERT INTO NflData.Game SELEC"), Store("MsSql"), NullLogger.Instance)
            .ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AStoredScriptOnAStoreWhoseKindHasNoReaderDrawsNothing()
    {
        LineageScriptEnds.Of(PipelineId, new GraphScript("load", "Files", "SELECT Id FROM Team"), Store("FileSystem"), NullLogger.Instance)
            .ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AGraphPipelineCarriesItsSqlStepsToTheLineage()
    {
        var graph = new GraphPipelineConfiguration
        {
            Name = "nfl",
            Components =
            [
                new PipelineComponentConfiguration { Name = "read", ComponentType = "SqlSource", DataStoreName = "NflDb", ScriptText = "SELECT Id FROM NflData.Team" },
                new PipelineComponentConfiguration { Name = "write", ComponentType = "ExecuteSql", DataStoreName = "NflDb", ScriptText = "DELETE FROM NflData.Game" },
            ],
        };

        PipelineContainers.Scripts(graph).Select(script => script.Component).ShouldBe(["read", "write"]);
    }
}
