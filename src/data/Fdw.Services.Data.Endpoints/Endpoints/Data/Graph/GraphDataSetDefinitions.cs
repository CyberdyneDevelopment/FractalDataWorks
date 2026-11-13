using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Services.Data.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Builds the parts of a Graph data set from what a create or update request states (FDW-5287): the fields out, the graph, and the intended sources,
/// each refused by its own coded result and none given a value the request left out.
/// </summary>
internal static class GraphDataSetDefinitions
{
    /// <summary>Checks the classifications and types the fields out state, by the rules every data set's fields follow.</summary>
    internal static IGenericResult ValidateFields(IEnumerable<int> classifications, IList<GraphFieldOutRequest>? fields, ILogger logger)
    {
        var classified = DataClassificationValidator.Validate(classifications, logger);
        if (classified.IsFailure || fields is null) return classified;

        return DataSetQueryHelper.ValidateFieldTypes([.. fields.Select(AsField)], logger);
    }

    /// <summary>Sets the fields out of a graph: the shared field rules (a new field states its nullability and whether it is filterable), then what feeds each.</summary>
    internal static IGenericResult Fields(
        IGraphDataSetConfiguration graph, IList<GraphFieldOutRequest> fields, DataSetRequestValues values, GraphDataSetStamp stamp, ILogger logger)
    {
        // A new field states whether callers may filter on it; the shared mapping reads it as stated, so a field that leaves it out is refused here.
        foreach (var field in fields)
        {
            var filterable = values.Required(field.IsFilterable, "field", field.Name, "isFilterable");
            if (filterable.IsFailure) return filterable;
        }

        var mapped = DataSetQueryHelper.MapFields([.. fields.Select(AsField)], graph.Fields, values);
        if (mapped.IsFailure) return mapped;

        if (fields.FirstOrDefault(f => (f.FedByComponent is null) != (f.FedByField is null)) is { } half)
        {
            return GenericResult.Failure(
                DataServiceResultCodes.ByName("GraphDataSetFieldFedByIncomplete"), logger,
                ResultDetails.Create("field", half.Name, "dataSet", graph.Name));
        }

        // The shared type's setter replaces the stored fields out with a copy of each, so what a field keeps that the shared type has no member for is taken first.
        var keptNotes = graph.GraphFields.GroupBy(f => f.Name, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First().Notes, StringComparer.Ordinal);
        graph.Fields = mapped.Value!;
        foreach (var stored in graph.GraphFields)
        {
            var stated = fields.First(f => string.Equals(f.Name, stored.Name, StringComparison.Ordinal));
            stored.FedByComponent = stated.FedByComponent;
            stored.FedByField = stated.FedByField;
            var notes = Notes(graph.Name, stated, keptNotes.GetValueOrDefault(stored.Name), stamp, logger);
            if (notes.IsFailure) return notes;
            stored.Notes = notes.Value!;
        }

        return GenericResult.Success();
    }

    /// <summary>Sets the components and links of a graph from the payload shapes a graph pipeline states.</summary>
    /// <remarks>
    /// The payload is read by the reader a graph pipeline uses, so a component's kind and settings mean what they mean there; its refusal of a
    /// setting or a kind that does not exist is the answer. A kind a data set graph refuses is refused by name, and then a setting a Graph component
    /// cannot hold is refused by name, never dropped.
    /// </remarks>
    internal static IGenericResult Body(
        IGraphDataSetConfiguration graph, IList<GraphComponentPayload> components, IList<GraphLinkPayload> links, ILogger logger)
    {
        // A kind a data set graph refuses is refused as that kind, whatever else the component states or leaves out.
        foreach (var component in components)
        {
            var kind = GraphDataSetRefusals.CheckKind(graph.Name, component.Name, component.Kind, logger);
            if (kind.IsFailure) return kind;
        }

        var problems = new List<GraphProblem>();
        var read = GraphPayloadReader.ReadDraft(new GraphPayload { Name = graph.Name, Components = components, Links = links }, Guid.CreateVersion7(), problems);
        if (read.IsFailure || read.Value is null) return read;
        if (problems.Count > 0) return GraphRefusal.Of<bool>(problems[0].Reason);

        graph.Components = [.. read.Value.Components.Select(GraphDataSetComponents.Read)];
        graph.Links = [.. read.Value.Links.Select(link => new GraphDataSetLinkConfiguration
        {
            Id = link.Id, FromComponent = link.FromComponent, FromOutput = link.FromOutput, ToComponent = link.ToComponent, ToInput = link.ToInput,
        })];

        var refused = GraphDataSetRefusals.Check(graph, logger);
        if (refused.IsFailure) return refused;

        foreach (var component in read.Value.Components)
        {
            if (GraphDataSetComponents.UnsupportedSettings(component) is [var setting, ..])
            {
                return GenericResult.Failure(
                    DataServiceResultCodes.ByName("GraphDataSetSettingNotSupported"), logger,
                    ResultDetails.Create("component", component.Name, "dataSet", graph.Name, "setting", setting));
            }
        }

        return GenericResult.Success();
    }

    /// <summary>Sets what the body of a create request states of a data set's fields out, graph and fields-out end, in the order saving runs them.</summary>
    /// <remarks>The first refusal is answered. It is the one place the create route and the check of the save-agreement build the data set from a body.</remarks>
    /// <param name="graph">The data set being built.</param>
    /// <param name="request">The body of the create request.</param>
    /// <param name="values">The request's values, for a member the kind declares a default for.</param>
    /// <param name="stamp">Who makes the change and when: the author of each new note.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>Success, or the first refusal.</returns>
    internal static IGenericResult Apply(GraphDataSetConfiguration graph, CreateGraphDataSetRequest request, DataSetRequestValues values, GraphDataSetStamp stamp, ILogger logger)
    {
        var steps = new Func<IGenericResult>[]
        {
            () => Fields(graph, request.FieldsOut, values, stamp, logger),
            () => Body(graph, request.Components, request.Links, logger),
            () => FieldsOut(graph, request.FieldsOutComponent, request.FieldsOutOutput, logger),
        };
        foreach (var step in steps)
        {
            var done = step();
            if (done.IsFailure) return done;
        }

        return GenericResult.Success();
    }

    /// <summary>Sets which component and output carry the fields out, when the request states them, and checks every field out against it.</summary>
    /// <remarks>Both are stated or neither; one left out keeps the stored one. Every field that names a component names this one.</remarks>
    internal static IGenericResult FieldsOut(IGraphDataSetConfiguration graph, string? component, string? output, ILogger logger)
    {
        if ((component is null) != (output is null))
        {
            return GenericResult.Failure(DataServiceResultCodes.ByName("GraphDataSetFieldsOutIncomplete"), logger, ResultDetails.Create("dataSet", graph.Name));
        }

        if (component is not null)
        {
            graph.FieldsOutComponent = component;
            graph.FieldsOutOutput = output;
        }

        return graph.GraphFields.FirstOrDefault(f => f.FedByComponent is not null && !string.Equals(f.FedByComponent, graph.FieldsOutComponent, StringComparison.Ordinal)) is { } elsewhere
            ? GenericResult.Failure(
                DataServiceResultCodes.ByName("GraphDataSetFieldFedByElsewhere"), logger,
                ResultDetails.Create("field", elsewhere.Name, "dataSet", graph.Name, "fedBy", elsewhere.FedByComponent).With("fieldsOut", graph.FieldsOutComponent))
            : GenericResult.Success();
    }

    /// <summary>Sets the intended sources of an abstract graph; a graph that has components states none.</summary>
    /// <remarks>
    /// The list replaces the stored one. An item that carries the id of a stored intended source keeps its id, author and time; one that carries none is new, with an id
    /// the server mints; an id that matches no stored one is refused by name.
    /// </remarks>
    internal static async Task<IGenericResult> Intents(
        IGraphDataSetConfiguration graph, IList<GraphIntendedSourceRequest> intended, IDataSetConfigurationProvider dataSets, GraphDataSetStamp stamp, ILogger logger, CancellationToken ct)
    {
        if (graph.Components.Count > 0 && intended.Count > 0)
        {
            return GenericResult.Failure(DataServiceResultCodes.ByName("GraphDataSetIntentOnBoundGraph"), logger, ResultDetails.Create("dataSet", graph.Name));
        }

        var kept = graph.Intents;
        var made = new List<GraphDataSetIntentConfiguration>(intended.Count);
        foreach (var (source, ordinal) in intended.Select((source, ordinal) => (source, ordinal)))
        {
            var intent = await Intent(graph.Name, source, ordinal, kept, dataSets, stamp, logger, ct).ConfigureAwait(false);
            if (intent.IsFailure) return intent;

            made.Add(intent.Value!);
        }

        graph.Intents = made;
        return GenericResult.Success();
    }

    /// <summary>Makes one intended source from what a request states, giving it the id, author and time of the stored one it names, or new ones.</summary>
    internal static async Task<IGenericResult<GraphDataSetIntentConfiguration>> Intent(
        string dataSet, GraphIntendedSourceRequest source, int ordinal, IList<GraphDataSetIntentConfiguration> kept, IDataSetConfigurationProvider dataSets,
        GraphDataSetStamp stamp, ILogger logger, CancellationToken ct)
    {
        var stored = StoredIntent(source, kept, stamp);
        if (source.Id is { } named && stored is null && stamp.Saves)
        {
            return GenericResult<GraphDataSetIntentConfiguration>.Failure(
                DataServiceResultCodes.ByName("GraphDataSetIntendedSourceNotFound"), logger, ResultDetails.Create("source", named.ToString("D"), "dataSet", dataSet));
        }

        var written = Written(stored is null, stored?.AuthorUserId, stored?.AuthoredAt, dataSet, stamp, logger);
        if (written.IsFailure) return written.ToNewResult<GraphDataSetIntentConfiguration>();

        var intent = new GraphDataSetIntentConfiguration
        {
            Id = stored is null ? Guid.CreateVersion7() : stored.Id, IntentKind = source.Kind, DataStoreName = source.DataStore, SourceDataSetName = source.DataSet,
            Note = source.Note, Ordinal = ordinal, AuthorUserId = written.Value.By, AuthoredAt = written.Value.At,
        };

        if (!GraphIntentKinds.ByName(source.Kind).NamesDataSet || string.IsNullOrWhiteSpace(source.DataSet)) return GenericResult<GraphDataSetIntentConfiguration>.Success(intent);

        var found = await dataSets.Get(source.DataSet, ct).ConfigureAwait(false);
        if (found.IsFailure) return found.ToNewResult<GraphDataSetIntentConfiguration>();
        if (found.Value is null) return GenericResult<GraphDataSetIntentConfiguration>.Failure(DataSetEndpointLog.DataSetNotFound(logger, source.DataSet));

        intent.SourceDataSetId = found.Value.Id;
        return GenericResult<GraphDataSetIntentConfiguration>.Success(intent);
    }

    // The stored intended source a request item names by id; none when it names none, and none for a check, which resolves no id.
    private static GraphDataSetIntentConfiguration? StoredIntent(GraphIntendedSourceRequest source, IList<GraphDataSetIntentConfiguration> kept, GraphDataSetStamp stamp)
        => source.Id is { } id && stamp.Saves ? kept.FirstOrDefault(intent => intent.Id == id) : null;

    // The notes a request sends replace the field's; one left out keeps the stored ones (none for a new field). The position is the order.
    private static IGenericResult<List<GraphDataSetFieldNoteConfiguration>> Notes(
        string dataSet, GraphFieldOutRequest stated, IList<GraphDataSetFieldNoteConfiguration>? kept, GraphDataSetStamp stamp, ILogger logger)
    {
        if (stated.Notes is not { } sent) return GenericResult<List<GraphDataSetFieldNoteConfiguration>>.Success([.. kept ?? []]);

        var made = new List<GraphDataSetFieldNoteConfiguration>(sent.Count);
        foreach (var (note, ordinal) in sent.Select((note, ordinal) => (note, ordinal)))
        {
            var one = Note(dataSet, stated.Name, note, ordinal, kept ?? [], stamp, logger);
            if (one.IsFailure) return one.ToNewResult<List<GraphDataSetFieldNoteConfiguration>>();

            made.Add(one.Value!);
        }

        return GenericResult<List<GraphDataSetFieldNoteConfiguration>>.Success(made);
    }

    // A note that carries the id of a stored note keeps its id, author and time (and who answered it and when, until it is answered again or not at all); one that carries
    // none is new. An id that matches no stored note is refused, never made into a new note.
    private static IGenericResult<GraphDataSetFieldNoteConfiguration> Note(
        string dataSet, string field, GraphFieldNoteRequest note, int ordinal, IList<GraphDataSetFieldNoteConfiguration> kept, GraphDataSetStamp stamp, ILogger logger)
    {
        var stored = note.Id is { } id && stamp.Saves ? kept.FirstOrDefault(k => k.Id == id) : null;
        if (note.Id is { } named && stored is null && stamp.Saves)
        {
            return GenericResult<GraphDataSetFieldNoteConfiguration>.Failure(
                DataServiceResultCodes.ByName("GraphDataSetNoteNotFound"), logger, ResultDetails.Create("note", named.ToString("D"), "field", field, "dataSet", dataSet));
        }

        var written = Written(stored is null, stored?.AuthorUserId, stored?.AuthoredAt, dataSet, stamp, logger);
        if (written.IsFailure) return written.ToNewResult<GraphDataSetFieldNoteConfiguration>();

        // Answered now when it was not before; a note answered before keeps who answered it and when, and one that is not answered has neither.
        var answered = note.IsAnswered
            ? Written(stored is not { IsAnswered: true }, stored?.AnsweredByUserId, stored?.AnsweredAt, dataSet, stamp, logger)
            : GenericResult<StampedBy>.Success(new StampedBy(null, null));
        if (answered.IsFailure) return answered.ToNewResult<GraphDataSetFieldNoteConfiguration>();

        return GenericResult<GraphDataSetFieldNoteConfiguration>.Success(new GraphDataSetFieldNoteConfiguration
        {
            Id = stored is null ? Guid.CreateVersion7() : stored.Id, NoteKind = note.Kind, Body = note.Body, IsAnswered = note.IsAnswered, Ordinal = ordinal,
            AuthorUserId = written.Value.By, AuthoredAt = written.Value.At, AnsweredByUserId = answered.Value.By, AnsweredAt = answered.Value.At,
        });
    }

    // Who did a thing and when: what is stored when it was done before, and the caller and now when it is done now. A check stamps nothing.
    private static IGenericResult<StampedBy> Written(bool doneNow, Guid? storedBy, DateTimeOffset? storedAt, string dataSet, GraphDataSetStamp stamp, ILogger logger)
    {
        if (!doneNow) return GenericResult<StampedBy>.Success(new StampedBy(storedBy, storedAt));

        var author = stamp.Author(dataSet, logger);
        return author.IsFailure
            ? author.ToNewResult<StampedBy>()
            : GenericResult<StampedBy>.Success(new StampedBy(author.Value, stamp.Saves ? stamp.Now : null));
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly record struct StampedBy(Guid? By, DateTimeOffset? At);

    private static CreateDataSetFieldRequest AsField(GraphFieldOutRequest f) => new()
    {
        Name = f.Name, Description = f.Description, ClassificationId = f.ClassificationId, DatumTypeId = f.DatumTypeId, DataType = f.DataType,
        IsNullable = f.IsNullable, IsKey = f.IsKey, IsRequired = f.IsRequired, IsIndexed = f.IsIndexed, IsFilterable = f.IsFilterable, MaxLength = f.MaxLength,
        CalculationName = f.CalculationName, IsJoinKey = f.IsJoinKey, Role = f.Role, Ordinal = f.Ordinal,
    };
}
