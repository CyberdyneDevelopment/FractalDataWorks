using System.Linq;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Writes a stored Graph data set the way <c>GET /datasets/graph/{name}/graph</c> answers it (FDW-5295): the fields out with their discussion and state, and the graph
/// in the shape a graph pipeline states one.
/// </summary>
/// <remarks>
/// The components and links are written by the graph pipeline's own writer (<see cref="GraphPayloadWriter"/>) from the stored body read
/// back as the pipeline's configuration, so the shape is the one the create route's reader takes and no second model of it exists.
/// </remarks>
internal static class GraphDataSetGraphPayloads
{
    /// <summary>Writes the whole answer: the fields out, and the graph with its fields-out component and intended sources.</summary>
    /// <param name="graph">The Graph data set, read whole.</param>
    /// <param name="header">The data set's detail as every other read of a data set answers it, with its category: the header members come from it.</param>
    /// <returns>The payload; an abstract data set has empty component and link lists.</returns>
    internal static GraphDataSetGraphPayload Open(IGraphDataSetConfiguration graph, DataSetDetailResponse header)
    {
        var body = GraphPayloadWriter.Write(new GraphPipelineConfiguration
        {
            Name = graph.Name,
            Components = [.. graph.Components.Select(GraphDataSetPipelineComponents.Write)],
            Links = [.. graph.Links.Select(link => new PipelineLinkConfiguration
            {
                Id = link.Id, FromComponent = link.FromComponent, FromOutput = link.FromOutput, ToComponent = link.ToComponent, ToInput = link.ToInput,
            })],
        });

        var payload = Header(graph, header, true);
        payload.FieldsOutComponent = graph.FieldsOutComponent;
        payload.FieldsOutOutput = graph.FieldsOutOutput;
        payload.Components = [.. body.Components];
        payload.Links = [.. body.Links];
        payload.IntendedSources = [.. graph.Intents.OrderBy(intent => intent.Ordinal).Select(IntendedSource)];
        return payload;
    }

    /// <summary>Writes the answer for a caller who may not read every source: the description and the fields, and why the graph is not answered.</summary>
    /// <param name="graph">The Graph data set, read whole.</param>
    /// <param name="header">The data set's detail as every other read of a data set answers it, with its category: the header members come from it.</param>
    /// <param name="restriction">Why the graph is not answered.</param>
    /// <returns>The payload; what names the graph or a source is left out (null), never emptied.</returns>
    internal static GraphDataSetGraphPayload Restricted(IGraphDataSetConfiguration graph, DataSetDetailResponse header, GraphDataSetRestrictionPayload restriction)
    {
        var payload = Header(graph, header, false);
        payload.Restriction = restriction;
        return payload;
    }

    private static GraphDataSetGraphPayload Header(IGraphDataSetConfiguration graph, DataSetDetailResponse header, bool withFeeds) => new()
    {
        Name = graph.Name,
        Description = graph.Description,
        ClassificationId = header.ClassificationId,
        Category = header.Category,
        Version = header.Version,
        RecordTypeName = header.RecordTypeName ?? string.Empty,
        Caching = header.Caching,
        FieldsOut = [.. graph.GraphFields.OrderBy(stored => stored.Ordinal).Select(stored => FieldOut(graph, stored, withFeeds))],
    };

    private static GraphFieldOutPayload FieldOut(IGraphDataSetConfiguration graph, GraphDataSetFieldConfiguration stored, bool withFeeds) => new()
    {
        Name = stored.Name,
        Description = stored.Description,
        ClassificationId = stored.ClassificationId,
        DatumTypeId = stored.DatumTypeId,
        DataType = stored.TypeName,
        IsNullable = stored.IsNullable,
        IsKey = stored.IsKey,
        IsRequired = stored.IsRequired,
        IsIndexed = stored.IsIndexed,
        IsFilterable = stored.IsFilterable,
        MaxLength = stored.MaxLength,
        CalculationName = stored.CalculationName,
        IsJoinKey = stored.IsJoinKey,
        Role = stored.Role,
        Ordinal = stored.Ordinal,
        FedByComponent = withFeeds ? stored.FedByComponent : null,
        FedByField = withFeeds ? stored.FedByField : null,
        Notes = [.. stored.Notes.OrderBy(note => note.Ordinal).Select(note => Note(note))],
        State = graph.StateOf(stored).Name,
    };

    /// <summary>Describes a stored note as the payload answers it: its content, its stable id, who wrote it and when, and who answered it and when.</summary>
    internal static GraphFieldNoteRequest Note(GraphDataSetFieldNoteConfiguration note) => new()
    {
        Kind = note.NoteKind, Body = note.Body, IsAnswered = note.IsAnswered, Id = note.Id, AuthorUserId = note.AuthorUserId, CreatedAt = note.AuthoredAt,
        AnsweredByUserId = note.AnsweredByUserId, AnsweredAt = note.AnsweredAt,
    };

    /// <summary>Describes a stored intended source as the payload answers it: what it names, its stable id, who stated it and when.</summary>
    internal static GraphIntendedSourceRequest IntendedSource(GraphDataSetIntentConfiguration intent) => new()
    {
        Kind = intent.IntentKind, DataStore = intent.DataStoreName, DataSet = intent.SourceDataSetName, Note = intent.Note,
        Id = intent.Id, AuthorUserId = intent.AuthorUserId, CreatedAt = intent.AuthoredAt,
    };
}
