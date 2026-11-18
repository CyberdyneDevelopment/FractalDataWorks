using System;
using System.Collections.Generic;
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
/// FDW-6391: lineage treats a data store's working area as if the pipeline never touched it. A container the pipeline names or a table a script names in a
/// working area adds no node and no edge, by name, whether or not the store holds it; a pipeline that reads Historical, loads tmp.X and writes Consolidation
/// is drawn from Historical to Consolidation. A store that names none answers as before.
/// </summary>
public class LineageWorkingAreaTests
{
    private static readonly Guid PipelineId = Guid.CreateVersion7();
    private static readonly Guid StoreId = Guid.CreateVersion7();

    private static IDataStoreImplementationConfiguration Store(string[] workingPaths, bool holdsWorkingTable)
    {
        var store = new Mock<IDataStoreImplementationConfiguration>(MockBehavior.Strict);
        store.SetupGet(s => s.Id).Returns(StoreId);
        store.SetupGet(s => s.Name).Returns("Warehouse");
        store.SetupGet(s => s.Implementation).Returns("MsSql");
        store.SetupGet(s => s.WorkingPaths).Returns([.. workingPaths]);
        store.SetupGet(s => s.Paths).Returns(
        [
            new DataPathConfiguration { Name = "hist", Containers = [new DataContainerConfiguration { Id = Guid.CreateVersion7(), Name = "Historical" }] },
            new DataPathConfiguration { Name = "con", Containers = [new DataContainerConfiguration { Id = Guid.CreateVersion7(), Name = "Consolidation" }] },
            new DataPathConfiguration
            {
                Name = "tmp",
                Containers = holdsWorkingTable ? [new DataContainerConfiguration { Id = Guid.CreateVersion7(), Name = "X" }] : [],
            },
        ]);
        return store.Object;
    }

    private sealed class Drawn
    {
        public MapState State { get; } = new();

        public List<MapNodeDto> Nodes { get; } = [];

        public List<MapEdgeDto> Edges { get; } = [];

        public Drawn()
        {
            State.AddNode(MapNodeIds.Pipeline(PipelineId));
        }

        public void End(IDataStoreImplementationConfiguration store, PipelineContainerRef reference, bool reads)
        {
            var stores = new Dictionary<string, IDataStoreImplementationConfiguration>(StringComparer.OrdinalIgnoreCase) { ["Warehouse"] = store };
            LineageContainerEnds.AddEnd(State, stores, PipelineId, reference, reads, Nodes, Edges, NullLogger.Instance);
        }

        public void Script(IDataStoreImplementationConfiguration store, string text)
        {
            foreach (var end in LineageScriptEnds.Of(PipelineId, new GraphScript("step", "Warehouse", text), store, NullLogger.Instance))
                End(store, end.Container, end.Reads);
        }

        public IEnumerable<string> Labels => Nodes.Select(node => node.Label);
    }

    [Fact]
    public void AContainerThePipelineNamesInAWorkingAreaDrawsNothing()
    {
        var drawn = new Drawn();

        drawn.End(Store(["tmp"], holdsWorkingTable: true), new PipelineContainerRef("Warehouse", "tmp", "X"), reads: false);

        drawn.Nodes.ShouldBeEmpty();
        drawn.Edges.ShouldBeEmpty();
        drawn.State.Unresolved.ShouldBe(0);
    }

    [Fact]
    public void AWorkingTableTheStoreDoesNotHoldDrawsNothingAndIsNotUnresolved()
    {
        var drawn = new Drawn();

        drawn.End(Store(["tmp"], holdsWorkingTable: false), new PipelineContainerRef("Warehouse", "tmp", "Dropped"), reads: true);

        drawn.Nodes.ShouldBeEmpty();
        drawn.Edges.ShouldBeEmpty();
        drawn.State.Unresolved.ShouldBe(0);
    }

    [Fact]
    public void TheWorkingAreaIsMatchedWhateverTheCaseOfThePathTheScriptNames()
    {
        var drawn = new Drawn();

        drawn.End(Store(["tmp"], holdsWorkingTable: true), new PipelineContainerRef("Warehouse", "TMP", "X"), reads: true);

        drawn.Nodes.ShouldBeEmpty();
        drawn.Edges.ShouldBeEmpty();
    }

    [Fact]
    public void AContainerNamedWithNoPathThatTheStoreHoldsOnlyInAWorkingAreaDrawsNothing()
    {
        var drawn = new Drawn();

        drawn.End(Store(["tmp"], holdsWorkingTable: true), new PipelineContainerRef("Warehouse", null, "X"), reads: false);

        drawn.Nodes.ShouldBeEmpty();
        drawn.Edges.ShouldBeEmpty();
    }

    [Fact]
    public void AStoreThatNamesNoWorkingAreaDrawsTheContainerAsBefore()
    {
        var drawn = new Drawn();

        drawn.End(Store([], holdsWorkingTable: true), new PipelineContainerRef("Warehouse", "tmp", "X"), reads: false);

        drawn.Labels.ShouldBe(["X"]);
        drawn.Edges.Count.ShouldBe(1);
    }

    [Fact]
    public void AStoreThatNamesNoWorkingAreaCountsAMissingTableUnresolvedAsBefore()
    {
        var drawn = new Drawn();

        drawn.End(Store([], holdsWorkingTable: false), new PipelineContainerRef("Warehouse", "tmp", "Dropped"), reads: true);

        drawn.Nodes.ShouldBeEmpty();
        drawn.State.Unresolved.ShouldBe(1);
    }

    [Fact]
    public void APipelineThatLoadsThroughAWorkingAreaIsDrawnFromItsRealSourceToWhereTheRowsLand()
    {
        var store = Store(["tmp"], holdsWorkingTable: false);
        var drawn = new Drawn();

        drawn.Script(store, "INSERT INTO tmp.X (Id) SELECT Id FROM hist.Historical");
        drawn.Script(store, "INSERT INTO con.Consolidation (Id) SELECT Id FROM tmp.X");

        drawn.Labels.OrderBy(label => label, StringComparer.Ordinal).ShouldBe(["Consolidation", "Historical"]);
        drawn.Edges.Count.ShouldBe(2);
        drawn.State.Unresolved.ShouldBe(0);
    }

    [Theory]
    [InlineData("DELETE FROM tmp.X")]
    [InlineData("TRUNCATE TABLE tmp.X")]
    [InlineData("SELECT Id INTO tmp.X FROM tmp.Y")]
    [InlineData("DROP TABLE tmp.X")]
    [InlineData("SELECT Id FROM tmp.X")]
    public void AWriteAReadATruncateADeleteOrADropOfAWorkingTableCountsAsNothing(string script)
    {
        var drawn = new Drawn();

        drawn.Script(Store(["tmp"], holdsWorkingTable: false), script);

        drawn.Nodes.ShouldBeEmpty();
        drawn.Edges.ShouldBeEmpty();
        drawn.State.Unresolved.ShouldBe(0);
    }

    [Fact]
    public void ADeleteOfAnOrdinaryTableIsStillDrawnAsAWrite()
    {
        var drawn = new Drawn();

        drawn.Script(Store(["tmp"], holdsWorkingTable: false), "DELETE FROM con.Consolidation");

        drawn.Labels.ShouldBe(["Consolidation"]);
    }
}
