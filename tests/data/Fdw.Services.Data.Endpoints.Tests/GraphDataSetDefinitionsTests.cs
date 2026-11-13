using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-5287: what a create or update of a Graph data set states becomes the parts of the data set, and each thing the kind refuses is refused by its
/// own code: a destination, an action or a loop whatever else it states, a setting a Graph component cannot hold, a field fed by half a name,
/// and intended sources on a graph that has components.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataSets.FDW5287")]
public sealed class GraphDataSetDefinitionsTests
{
    private static readonly GraphDataSetStamp Stamp = GraphDataSetStamp.Saving(Guid.Parse("00000000-0000-0000-0000-0000000000a1"));

    private static GraphDataSetConfiguration Graph() => new() { Name = "GasPrices" };

    private static GraphComponentPayload Source(string name) => new()
    {
        Name = name,
        Kind = "DataSetSource",
        Settings = new Dictionary<string, object?>(System.StringComparer.OrdinalIgnoreCase) { ["dataSet"] = "Teams", ["batchSize"] = 1000 },
    };

    [Theory]
    [InlineData("ContainerDestination", typeof(GraphDataSetDestinationRefusedCode))]
    [InlineData("DataSetDestination", typeof(GraphDataSetDestinationRefusedCode))]
    [InlineData("ExecuteSql", typeof(GraphDataSetActionRefusedCode))]
    [InlineData("SetVariable", typeof(GraphDataSetActionRefusedCode))]
    [InlineData("ReadDataOptions", typeof(GraphDataSetActionRefusedCode))]
    [InlineData("ForEach", typeof(GraphDataSetLoopRefusedCode))]
    public void AKindTheGraphRefusesIsRefusedAsThatKindWhateverItStates(string kind, System.Type code)
    {
        var graph = Graph();

        var result = GraphDataSetDefinitions.Body(graph, [new GraphComponentPayload { Name = "Out", Kind = kind }], [], NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType(code);
    }

    [Fact]
    public void ASourceIsReadAndKeptWithItsSettings()
    {
        var graph = Graph();

        var result = GraphDataSetDefinitions.Body(graph, [Source("Teams")], [], NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        var component = graph.Components.Single();
        component.Name.ShouldBe("Teams");
        component.ComponentType.ShouldBe("DataSetSource");
        component.DataSetName.ShouldBe("Teams");
        component.BatchSize.ShouldBe(1000);
    }

    [Fact]
    public void ALinkIsKeptWithItsEnds()
    {
        var graph = Graph();
        var link = new GraphLinkPayload { From = new GraphNodePayload { Component = "Teams", Node = "output" }, To = new GraphNodePayload { Component = "Sort", Node = "input" } };

        GraphDataSetDefinitions.Body(graph, [Source("Teams")], [link], NullLogger.Instance).IsSuccess.ShouldBeTrue();

        var kept = graph.Links.Single();
        (kept.FromComponent, kept.FromOutput, kept.ToComponent, kept.ToInput).ShouldBe(("Teams", "output", "Sort", "input"));
    }

    [Fact]
    public void ASettingTheGraphComponentCannotHoldIsRefusedNamingItAndNotDropped()
    {
        var graph = Graph();
        var source = Source("Teams");
        source.Settings["stage"] = 2;

        var result = GraphDataSetDefinitions.Body(graph, [source], [], NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetSettingNotSupportedCode>();
    }

    [Fact]
    public void AKindThatDoesNotExistIsRefusedByTheGraphReader()
    {
        var result = GraphDataSetDefinitions.Body(Graph(), [new GraphComponentPayload { Name = "X", Kind = "NoSuchKind" }], [], NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task AGraphThatHasComponentsAndIntendedSourcesIsRefused()
    {
        var graph = Graph();
        graph.Components.Add(new GraphDataSetComponentConfiguration { Name = "Teams", ComponentType = "DataSetSource" });

        var result = await GraphDataSetDefinitions.Intents(
            graph, [new GraphIntendedSourceRequest { Kind = "DataStore", DataStore = "Gas" }],
            new Mock<IDataSetConfigurationProvider>().Object, Stamp, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetIntentOnBoundGraphCode>();
    }

    [Fact]
    public async Task AnAbstractGraphKeepsItsIntendedDataStoreInOrder()
    {
        var graph = Graph();

        var result = await GraphDataSetDefinitions.Intents(
            graph,
            [new GraphIntendedSourceRequest { Kind = "DataStore", DataStore = "Gas", Note = "prices" }, new GraphIntendedSourceRequest { Kind = "DataStore", DataStore = "Power" }],
            new Mock<IDataSetConfigurationProvider>().Object, Stamp, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        graph.Intents.Select(i => (i.DataStoreName, i.Ordinal)).ShouldBe([("Gas", 0), ("Power", 1)]);
        graph.Intents[0].Note.ShouldBe("prices");
    }

    [Fact]
    public void AFieldFedByHalfANameIsRefused()
    {
        var graph = Graph();
        var field = new GraphFieldOutRequest { Name = "Amount", IsNullable = false, IsFilterable = true, FedByComponent = "Join1" };

        var result = GraphDataSetDefinitions.Fields(graph, [field], new DataSetRequestValues("GasPrices", NullLogger.Instance), Stamp, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetFieldFedByIncompleteCode>();
    }

    [Fact]
    public void FieldsOutKeepWhatFeedsThem()
    {
        var graph = Graph();
        var fed = new GraphFieldOutRequest { Name = "Amount", IsNullable = false, IsFilterable = true, FedByComponent = "Join1", FedByField = "Total" };
        var open = new GraphFieldOutRequest { Name = "Note", IsNullable = true, IsFilterable = false };

        var result = GraphDataSetDefinitions.Fields(graph, [fed, open], new DataSetRequestValues("GasPrices", NullLogger.Instance), Stamp, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        graph.GraphFields.Single(f => f.Name == "Amount").FedByField.ShouldBe("Total");
        graph.GraphFields.Single(f => f.Name == "Note").FedByComponent.ShouldBeNull();
    }

    [Fact]
    public void ANewFieldThatLeavesOutItsNullabilityIsRefused()
    {
        var graph = Graph();

        var result = GraphDataSetDefinitions.Fields(
            graph, [new GraphFieldOutRequest { Name = "Amount", IsFilterable = true }], new DataSetRequestValues("GasPrices", NullLogger.Instance), Stamp, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ATerminalNamedByHalfIsRefused()
    {
        var result = GraphDataSetDefinitions.FieldsOut(Graph(), "Join1", null, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetFieldsOutIncompleteCode>();
    }

    [Fact]
    public void AFieldFedByAnotherComponentThanTheTerminalIsRefused()
    {
        var graph = Graph();
        graph.GraphFields.Add(new GraphDataSetFieldConfiguration { Name = "Amount", FedByComponent = "Calc1", FedByField = "Amount" });

        var result = GraphDataSetDefinitions.FieldsOut(graph, "Join1", "output", NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetFieldFedByElsewhereCode>();
    }

    [Fact]
    public void TheTerminalIsKeptWhenEveryFieldIsFedByItsComponent()
    {
        var graph = Graph();
        graph.GraphFields.Add(new GraphDataSetFieldConfiguration { Name = "Amount", FedByComponent = "Join1", FedByField = "Amount" });

        GraphDataSetDefinitions.FieldsOut(graph, "Join1", "output", NullLogger.Instance).IsSuccess.ShouldBeTrue();

        (graph.FieldsOutComponent, graph.FieldsOutOutput).ShouldBe(("Join1", "output"));
    }

    [Fact]
    public void AnUpdateThatStatesNoTerminalKeepsTheStoredOne()
    {
        var graph = Graph();
        graph.FieldsOutComponent = "Join1";
        graph.FieldsOutOutput = "output";

        GraphDataSetDefinitions.FieldsOut(graph, null, null, NullLogger.Instance).IsSuccess.ShouldBeTrue();

        graph.FieldsOutComponent.ShouldBe("Join1");
    }

    [Fact]
    public void NotesOnAFieldAreKeptInTheOrderTheyAreSent()
    {
        var graph = Graph();
        var field = new GraphFieldOutRequest
        {
            Name = "Amount", IsNullable = false, IsFilterable = true,
            Notes = [new GraphFieldNoteRequest { Kind = "Question", Body = "Which tariff?" }, new GraphFieldNoteRequest { Kind = "Reply", Body = "Spot" }],
        };

        GraphDataSetDefinitions.Fields(graph, [field], new DataSetRequestValues("GasPrices", NullLogger.Instance), Stamp, NullLogger.Instance).IsSuccess.ShouldBeTrue();

        graph.GraphFields.Single().Notes.Select(n => (n.NoteKind, n.Body, n.Ordinal)).ShouldBe([("Question", "Which tariff?", 0), ("Reply", "Spot", 1)]);
    }

    [Fact]
    public void ReplacingFieldsWithoutNotesKeepsTheStoredNotesOfAFieldThatStays()
    {
        var graph = Graph();
        graph.GraphFields.Add(new GraphDataSetFieldConfiguration { Name = "Amount", Notes = [new GraphDataSetFieldNoteConfiguration { NoteKind = "Question", Body = "Which tariff?" }] });

        GraphDataSetDefinitions.Fields(
            graph, [new GraphFieldOutRequest { Name = "Amount", IsNullable = false, IsFilterable = true }], new DataSetRequestValues("GasPrices", NullLogger.Instance), Stamp, NullLogger.Instance)
            .IsSuccess.ShouldBeTrue();

        graph.GraphFields.Single().Notes.Single().Body.ShouldBe("Which tariff?");
    }

    [Fact]
    public void AnEmptyListOfNotesClearsThem()
    {
        var graph = Graph();
        graph.GraphFields.Add(new GraphDataSetFieldConfiguration { Name = "Amount", Notes = [new GraphDataSetFieldNoteConfiguration { NoteKind = "Question", Body = "Which tariff?" }] });

        GraphDataSetDefinitions.Fields(
            graph, [new GraphFieldOutRequest { Name = "Amount", IsNullable = false, IsFilterable = true, Notes = [] }], new DataSetRequestValues("GasPrices", NullLogger.Instance), Stamp, NullLogger.Instance)
            .IsSuccess.ShouldBeTrue();

        graph.GraphFields.Single().Notes.ShouldBeEmpty();
    }
}
