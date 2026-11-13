using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-5295: a stored Graph data set is answered in the shapes a request states: the graph the pipeline's own reader took in comes back out of the pipeline's own
/// writer, the fields carry their discussion in order and their worked-out state, and a caller who may not read the sources gets the fields and a restriction.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataSets.FDW5295")]
public sealed class GraphDataSetGraphPayloadsTests
{
    private static GraphComponentPayload Source(string name, int x, int y) => new()
    {
        Name = name,
        Kind = "DataSetSource",
        Settings = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["dataSet"] = "Teams", ["batchSize"] = 1000 },
        Layout = new GraphLayoutPayload { X = x, Y = y },
    };

    private static GraphLinkPayload Link(string from, string to) => new()
    {
        From = new GraphNodePayload { Component = from, Node = "output" }, To = new GraphNodePayload { Component = to, Node = "input" },
    };

    private static GraphDataSetGraphPayload Opened(GraphDataSetConfiguration graph) => GraphDataSetGraphPayloads.Open(graph, DataSetQueryHelper.MapToDetail(graph));

    private static GraphDataSetGraphPayload Restrict(GraphDataSetConfiguration graph, GraphDataSetRestrictionPayload restriction) =>
        GraphDataSetGraphPayloads.Restricted(graph, DataSetQueryHelper.MapToDetail(graph), restriction);

    private static GraphDataSetConfiguration Stored(params GraphComponentPayload[] components)
    {
        var graph = new GraphDataSetConfiguration { Name = "GasPrices", Description = "prices" };
        GraphDataSetDefinitions.Body(graph, components, [], NullLogger.Instance).IsSuccess.ShouldBeTrue();
        return graph;
    }

    private static GraphDataSetFieldConfiguration Field(string name, int ordinal, string? fedByComponent = null, string? fedByField = null) => new()
    {
        Name = name, Ordinal = ordinal, FedByComponent = fedByComponent, FedByField = fedByField,
    };

    private static GraphDataSetFieldConfiguration Noted(GraphDataSetFieldConfiguration stored, string kind, string body, int ordinal, bool answered)
    {
        stored.Notes.Add(new GraphDataSetFieldNoteConfiguration { NoteKind = kind, Body = body, Ordinal = ordinal, IsAnswered = answered });
        return stored;
    }

    [Fact]
    public void AGraphTheCreateReaderTookInComesBackWithItsKindSettingsAndLayout()
    {
        var payload = Opened(Stored(Source("Teams", 40, 80)));

        var component = payload.Components.ShouldNotBeNull().Single();
        (component.Name, component.Kind).ShouldBe(("Teams", "DataSetSource"));
        component.Settings["dataSet"].ShouldBe("Teams");
        component.Settings["batchSize"].ShouldBe(1000);
        (component.Layout.ShouldNotBeNull().X, component.Layout.Y).ShouldBe((40, 80));
        payload.Restriction.ShouldBeNull();
    }

    [Fact]
    public void LinksComeBackWithTheirEnds()
    {
        var graph = Stored(Source("Teams", 0, 0), Source("Sorted", 10, 0));
        graph.Links.Add(new GraphDataSetLinkConfiguration { FromComponent = "Teams", FromOutput = "output", ToComponent = "Sorted", ToInput = "input" });

        var link = Opened(graph).Links.ShouldNotBeNull().Single();

        (link.From.Component, link.From.Node, link.To.Component, link.To.Node).ShouldBe(("Teams", "output", "Sorted", "input"));
    }

    [Fact]
    public void AnAbstractDataSetAnswersEmptyListsAndItsIntent()
    {
        var graph = new GraphDataSetConfiguration { Name = "Abstract" };
        graph.Intents.Add(new GraphDataSetIntentConfiguration { IntentKind = "DataStore", DataStoreName = "Gas", Note = "prices", Ordinal = 0 });

        var payload = Opened(graph);

        payload.Components.ShouldNotBeNull().ShouldBeEmpty();
        payload.Links.ShouldNotBeNull().ShouldBeEmpty();
        var intent = payload.IntendedSources.ShouldNotBeNull().Single();
        (intent.Kind, intent.DataStore, intent.Note).ShouldBe(("DataStore", "Gas", "prices"));
    }

    [Fact]
    public void NotesComeBackInTheirStoredOrderAndTheStateIsWorkedOut()
    {
        var graph = new GraphDataSetConfiguration { Name = "Abstract" };
        graph.Intents.Add(new GraphDataSetIntentConfiguration { IntentKind = "DataStore", DataStoreName = "Gas", Ordinal = 0 });
        graph.GraphFields.Add(Noted(Noted(Field("Asked", 0), "Reply", "second", 1, false), "Question", "first", 0, false));
        graph.GraphFields.Add(Noted(Field("Answered", 1), "Question", "done", 0, true));
        graph.GraphFields.Add(Field("Fed", 2, "Join1", "Total"));

        var fields = Opened(graph).FieldsOut;

        var asked = fields.Single(f => f.Name == "Asked");
        asked.Notes.Select(n => (n.Kind, n.Body, n.IsAnswered)).ShouldBe([("Question", "first", false), ("Reply", "second", false)]);
        asked.State.ShouldBe("QuestionOpen");
        fields.Single(f => f.Name == "Answered").State.ShouldBe("Intended");
        fields.Single(f => f.Name == "Fed").State.ShouldBe("Defined");
        fields.Select(f => f.Name).ShouldBe(["Asked", "Answered", "Fed"]);
    }

    [Fact]
    public void ARestrictedAnswerKeepsTheFieldsAndDescriptionAndLeavesTheGraphOutNotEmpty()
    {
        var graph = Stored(Source("Teams", 0, 0));
        graph.FieldsOutComponent = "Teams";
        graph.FieldsOutOutput = "output";
        graph.GraphFields.Add(Noted(Field("Amount", 0, "Teams", "Total"), "Question", "why", 0, false));
        var restriction = new GraphDataSetRestrictionPayload { Code = "DATA-21078", Message = "not readable" };

        var payload = Restrict(graph, restriction);

        payload.Description.ShouldBe("prices");
        payload.Restriction.ShouldBeSameAs(restriction);
        payload.Components.ShouldBeNull();
        payload.Links.ShouldBeNull();
        payload.IntendedSources.ShouldBeNull();
        payload.FieldsOutComponent.ShouldBeNull();
        payload.FieldsOutOutput.ShouldBeNull();
        var amount = payload.FieldsOut.Single();
        amount.FedByComponent.ShouldBeNull();
        amount.FedByField.ShouldBeNull();
        amount.Notes.Single().Body.ShouldBe("why");
    }

    [Fact]
    public void ARestrictedAbstractDataSetStillAnswersNullListsWhereTheOpenOneAnswersEmptyOnes()
    {
        var abstractGraph = new GraphDataSetConfiguration { Name = "Abstract" };
        abstractGraph.Intents.Add(new GraphDataSetIntentConfiguration { IntentKind = "DataStore", DataStoreName = "Gas", Ordinal = 0 });

        var restricted = Restrict(abstractGraph, new GraphDataSetRestrictionPayload { Code = "DATA-21078", Message = "not readable" });

        restricted.Components.ShouldBeNull();
        restricted.Links.ShouldBeNull();
        restricted.IntendedSources.ShouldBeNull();
        Opened(abstractGraph).Components.ShouldNotBeNull().ShouldBeEmpty();
    }
}
