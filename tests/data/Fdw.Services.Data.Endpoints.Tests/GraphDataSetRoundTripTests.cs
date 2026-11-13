using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-5451: what the get-graph route answers can be sent back to create unchanged. A stored Graph data set is written as the route answers it, serialized, read as
/// the create request, built by the create steps, and the result is the data set that was stored.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataSets.FDW5451")]
public sealed class GraphDataSetRoundTripTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    // The shapes compared hold tuples, which serialize their members as fields.
    private static readonly JsonSerializerOptions Compared = new(JsonSerializerDefaults.Web) { IncludeFields = true };

    private static GraphComponentPayload Source(string name, int x, int y) => new()
    {
        Name = name,
        Kind = "DataSetSource",
        Settings = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["dataSet"] = "Teams", ["batchSize"] = 500 },
        Layout = new GraphLayoutPayload { X = x, Y = y },
    };

    private static GraphFieldOutRequest Field(string name, int ordinal, string? fedByComponent, string? fedByField, params GraphFieldNoteRequest[] notes) => new()
    {
        Name = name, Ordinal = ordinal, IsNullable = ordinal > 0, IsFilterable = true, DataType = string.Empty, Role = "Measure", MaxLength = 12,
        FedByComponent = fedByComponent, FedByField = fedByField, Notes = [.. notes],
    };

    private static async Task<GraphDataSetConfiguration> Build(CreateGraphDataSetRequest request)
    {
        var graph = new GraphDataSetConfiguration { Name = request.Name };
        var values = new DataSetRequestValues(request.Name, NullLogger.Instance);

        GraphDataSetDefinitions.Fields(graph, request.FieldsOut, values, GraphDataSetStamp.Checking, NullLogger.Instance).IsSuccess.ShouldBeTrue();
        GraphDataSetDefinitions.Body(graph, request.Components, request.Links, NullLogger.Instance).IsSuccess.ShouldBeTrue();
        GraphDataSetDefinitions.FieldsOut(graph, request.FieldsOutComponent, request.FieldsOutOutput, NullLogger.Instance).IsSuccess.ShouldBeTrue();
        (await GraphDataSetDefinitions.Intents(
            graph, request.IntendedSources, new Mock<IDataSetConfigurationProvider>().Object, GraphDataSetStamp.Checking, NullLogger.Instance, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        return graph;
    }

    private static CreateGraphDataSetRequest Bound() => new()
    {
        Name = "GasPrices",
        FieldsOut =
        [
            Field("Amount", 0, "Teams", "Total", new GraphFieldNoteRequest { Kind = "Question", Body = "net or gross?", IsAnswered = false }, new GraphFieldNoteRequest { Kind = "Reply", Body = "net" }),
            Field("Day", 1, "Teams", "Day"),
        ],
        FieldsOutComponent = "Teams",
        FieldsOutOutput = "output",
        Components = [Source("Teams", 40, 80), Source("Sorted", 200, 80)],
        Links = [new GraphLinkPayload { From = new GraphNodePayload { Component = "Teams", Node = "output" }, To = new GraphNodePayload { Component = "Sorted", Node = "input" } }],
    };

    private static CreateGraphDataSetRequest Abstract() => new()
    {
        Name = "Abstract",
        FieldsOut = [Field("Amount", 0, null, null, new GraphFieldNoteRequest { Kind = "Question", Body = "from where?", IsAnswered = false })],
        IntendedSources =
        [
            new GraphIntendedSourceRequest { Kind = "DataStore", DataStore = "Gas", Note = "prices" },
            new GraphIntendedSourceRequest { Kind = "DataStore", DataStore = "Power" },
        ],
    };

    private static void WithHeader(GraphDataSetConfiguration graph)
    {
        graph.ClassificationId = 2;
        graph.Version = "1.2.0";
        graph.RecordTypeName = "GasPriceRow";
        graph.Caching = new CachingConfiguration { Enabled = true, DurationMinutes = 5, KeyPattern = "gas-{day}" };
    }

    private static (CreateGraphDataSetRequest Sent, GraphDataSetGraphPayload Read) Sent(GraphDataSetConfiguration stored)
    {
        WithHeader(stored);
        var read = GraphDataSetGraphPayloads.Open(stored, DataSetQueryHelper.MapToDetail(stored));
        var json = JsonSerializer.Serialize(read, Web);
        var sent = JsonSerializer.Deserialize<CreateGraphDataSetRequest>(json, Web).ShouldNotBeNull();
        return (sent, read);
    }

    private static object Shape(GraphDataSetConfiguration graph) => new
    {
        graph.Name,
        graph.FieldsOutComponent,
        graph.FieldsOutOutput,
        Fields = graph.GraphFields.OrderBy(f => f.Ordinal).Select(f => (f.Name, f.Ordinal, f.IsNullable, f.IsFilterable, f.Role, f.MaxLength, f.FedByComponent, f.FedByField,
            Notes: string.Join("|", f.Notes.OrderBy(n => n.Ordinal).Select(n => $"{n.NoteKind}:{n.Body}:{n.IsAnswered}")))).ToList(),
        Components = graph.Components.Select(c => (c.Name, c.ComponentType, c.DataSetName, c.BatchSize, c.LayoutX, c.LayoutY)).ToList(),
        Links = graph.Links.Select(l => (l.FromComponent, l.FromOutput, l.ToComponent, l.ToInput)).ToList(),
        Intents = graph.Intents.OrderBy(i => i.Ordinal).Select(i => (i.IntentKind, i.DataStoreName, i.Note, i.Ordinal)).ToList(),
    };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AReadSentBackToCreateBuildsTheDataSetThatWasStored(bool bound)
    {
        var stored = await Build(bound ? Bound() : Abstract());

        var (sent, _) = Sent(stored);
        var rebuilt = await Build(sent);

        JsonSerializer.Serialize(Shape(rebuilt), Compared).ShouldBe(JsonSerializer.Serialize(Shape(stored), Compared));
    }

    [Fact]
    public async Task TheHeaderMembersAreAnsweredAndSentBackAsStored()
    {
        var stored = await Build(Bound());

        var (sent, read) = Sent(stored);

        (read.ClassificationId, read.Version, read.RecordTypeName).ShouldBe((2, "1.2.0", "GasPriceRow"));
        var caching = read.Caching.ShouldNotBeNull();
        (caching.Enabled, caching.DurationMinutes, caching.KeyPattern).ShouldBe((true, 5, "gas-{day}"));
        (sent.ClassificationId, sent.Version, sent.RecordTypeName).ShouldBe((2, "1.2.0", "GasPriceRow"));
        var mapped = DataSetQueryHelper.MapCaching(sent.Caching, GraphDataSetConfiguration.ImplementationName, new DataSetRequestValues("GasPrices", NullLogger.Instance));
        mapped.IsSuccess.ShouldBeTrue();
        var kept = mapped.Value.ShouldNotBeNull();
        (kept.Enabled, kept.DurationMinutes, kept.KeyPattern).ShouldBe((true, 5, "gas-{day}"));
    }

    [Fact]
    public async Task TheCategoryIsTheNameTheDetailCarries()
    {
        var stored = await Build(Bound());
        var detail = DataSetQueryHelper.MapToDetail(stored);
        detail.Category = "Energy";

        GraphDataSetGraphPayloads.Open(stored, detail).Category.ShouldBe("Energy");
        GraphDataSetGraphPayloads.Restricted(stored, detail, new GraphDataSetRestrictionPayload { Code = "DATA-21078", Message = "m" }).Category.ShouldBe("Energy");
    }
}
