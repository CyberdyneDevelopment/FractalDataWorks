using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The steps the routes that work on one note or one intended source of a Graph data set share (FDW-5452): load it whole, find the part, save it.</summary>
/// <remarks>
/// A part that does not exist is refused by its own coded result and never made: a field, a note and an intended source are each found by name or by stable id.
/// Saving is the update every other change to a data set takes, so a change is versioned like any other.
/// </remarks>
internal static class GraphDataSetEdits
{
    /// <summary>Loads a Graph data set whole; one that does not exist is not found, and one of another kind is refused with its own code.</summary>
    internal static async Task<IGenericResult<GraphDataSetConfiguration>> Load(IDataSetConfigurationProvider dataSets, string name, ILogger logger, CancellationToken ct)
    {
        var loaded = await dataSets.Get(name, ConfigurationLevels.WholeAggregate, ct).ConfigureAwait(false);
        if (loaded.IsFailure) return loaded.ToNewResult<GraphDataSetConfiguration>();
        if (loaded.Value is null) return GenericResult<GraphDataSetConfiguration>.Failure(DataSetEndpointLog.DataSetNotFound(logger, name));

        return loaded.Value is GraphDataSetConfiguration graph
            ? GenericResult<GraphDataSetConfiguration>.Success(graph)
            : GenericResult<GraphDataSetConfiguration>.Failure(
                DataServiceResultCodes.ByName("GraphDataSetNotGraph"), logger, ResultDetails.Create("dataSet", name, "implementation", loaded.Value.Implementation));
    }

    /// <summary>Saves a Graph data set the way the update of every data set does.</summary>
    internal static async Task<IGenericResult> Save(IDataSetConfigurationProvider dataSets, GraphDataSetConfiguration graph, ILogger logger, CancellationToken ct)
    {
        DataSetEndpointLog.UpdatingDataSet(logger, graph.Name);
        var saved = await dataSets.Update(graph, "DataSet", graph.Implementation, graph.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            DataSetEndpointLog.DataSetUpdateFailed(logger, graph.Name, "Save failed");
            return saved;
        }

        DataSetEndpointLog.DataSetUpdated(logger, graph.Name);
        return saved;
    }

    /// <summary>Finds a field out by name.</summary>
    internal static IGenericResult<GraphDataSetFieldConfiguration> Field(GraphDataSetConfiguration graph, string field, ILogger logger)
        => graph.GraphFields.FirstOrDefault(f => string.Equals(f.Name, field, StringComparison.Ordinal)) is { } found
            ? GenericResult<GraphDataSetFieldConfiguration>.Success(found)
            : GenericResult<GraphDataSetFieldConfiguration>.Failure(
                DataServiceResultCodes.ByName("GraphDataSetFieldNotFound"), logger, ResultDetails.Create("field", field, "dataSet", graph.Name));

    /// <summary>Finds a note of a field out by its stable id.</summary>
    internal static IGenericResult<GraphDataSetFieldNoteConfiguration> Note(GraphDataSetConfiguration graph, GraphDataSetFieldConfiguration field, Guid noteId, ILogger logger)
        => field.Notes.FirstOrDefault(n => n.Id == noteId) is { } found
            ? GenericResult<GraphDataSetFieldNoteConfiguration>.Success(found)
            : GenericResult<GraphDataSetFieldNoteConfiguration>.Failure(
                DataServiceResultCodes.ByName("GraphDataSetNoteNotFound"), logger, ResultDetails.Create("note", noteId.ToString("D"), "field", field.Name, "dataSet", graph.Name));

    /// <summary>Finds an intended source by its stable id.</summary>
    internal static IGenericResult<GraphDataSetIntentConfiguration> IntendedSource(GraphDataSetConfiguration graph, Guid sourceId, ILogger logger)
        => graph.Intents.FirstOrDefault(i => i.Id == sourceId) is { } found
            ? GenericResult<GraphDataSetIntentConfiguration>.Success(found)
            : GenericResult<GraphDataSetIntentConfiguration>.Failure(
                DataServiceResultCodes.ByName("GraphDataSetIntendedSourceNotFound"), logger, ResultDetails.Create("source", sourceId.ToString("D"), "dataSet", graph.Name));

    /// <summary>Adds one note to a field out and answers it, with its stable id, author and time; the field's other notes are left as they are.</summary>
    internal static async Task<IGenericResult<GraphFieldNoteRequest>> AddNote(
        IDataSetConfigurationProvider dataSets, AddGraphFieldNoteRequest request, GraphDataSetStamp stamp, ILogger logger, CancellationToken ct)
    {
        var loaded = await Load(dataSets, request.Name, logger, ct).ConfigureAwait(false);
        if (loaded.IsFailure) return loaded.ToNewResult<GraphFieldNoteRequest>();

        var field = Field(loaded.Value!, request.Field, logger);
        if (field.IsFailure) return field.ToNewResult<GraphFieldNoteRequest>();

        var author = stamp.Author(request.Name, logger);
        if (author.IsFailure) return author.ToNewResult<GraphFieldNoteRequest>();

        var note = new GraphDataSetFieldNoteConfiguration
        {
            Id = Guid.CreateVersion7(), NoteKind = request.Kind, Body = request.Body, IsAnswered = false,
            Ordinal = field.Value!.Notes.Count == 0 ? 0 : field.Value.Notes.Max(n => n.Ordinal) + 1, AuthorUserId = author.Value, AuthoredAt = stamp.Now,
        };
        field.Value.Notes.Add(note);

        var saved = await Save(dataSets, loaded.Value!, logger, ct).ConfigureAwait(false);
        return saved.IsFailure ? saved.ToNewResult<GraphFieldNoteRequest>() : GenericResult<GraphFieldNoteRequest>.Success(GraphDataSetGraphPayloads.Note(note));
    }

    /// <summary>Marks one note answered and records who answered it and when; a kind that cannot be answered is refused, and a note already answered keeps its answer and nothing is saved.</summary>
    internal static async Task<IGenericResult<GraphFieldNoteRequest>> AnswerNote(
        IDataSetConfigurationProvider dataSets, GraphFieldNoteRouteRequest request, GraphDataSetStamp stamp, ILogger logger, CancellationToken ct)
    {
        var loaded = await Load(dataSets, request.Name, logger, ct).ConfigureAwait(false);
        if (loaded.IsFailure) return loaded.ToNewResult<GraphFieldNoteRequest>();

        var field = Field(loaded.Value!, request.Field, logger);
        if (field.IsFailure) return field.ToNewResult<GraphFieldNoteRequest>();

        var found = Note(loaded.Value!, field.Value!, request.NoteId, logger);
        if (found.IsFailure) return found.ToNewResult<GraphFieldNoteRequest>();

        var note = found.Value!;
        if (!GraphFieldNoteKinds.ByName(note.NoteKind).CanBeAnswered)
        {
            return GenericResult<GraphFieldNoteRequest>.Failure(
                DataServiceResultCodes.ByName("GraphDataSetNoteCannotBeAnswered"), logger,
                ResultDetails.Create("note", note.Id.ToString("D"), "field", request.Field, "dataSet", request.Name).With("kind", note.NoteKind));
        }

        if (note.IsAnswered) return GenericResult<GraphFieldNoteRequest>.Success(GraphDataSetGraphPayloads.Note(note));

        var answerer = stamp.Author(request.Name, logger);
        if (answerer.IsFailure) return answerer.ToNewResult<GraphFieldNoteRequest>();

        note.IsAnswered = true;
        note.AnsweredByUserId = answerer.Value;
        note.AnsweredAt = stamp.Now;

        var saved = await Save(dataSets, loaded.Value!, logger, ct).ConfigureAwait(false);
        return saved.IsFailure ? saved.ToNewResult<GraphFieldNoteRequest>() : GenericResult<GraphFieldNoteRequest>.Success(GraphDataSetGraphPayloads.Note(note));
    }

    /// <summary>Removes one note from a field out; the field's other notes are left as they are.</summary>
    internal static async Task<IGenericResult> RemoveNote(
        IDataSetConfigurationProvider dataSets, GraphFieldNoteRouteRequest request, ILogger logger, CancellationToken ct)
    {
        var loaded = await Load(dataSets, request.Name, logger, ct).ConfigureAwait(false);
        if (loaded.IsFailure) return loaded;

        var field = Field(loaded.Value!, request.Field, logger);
        if (field.IsFailure) return field;

        var note = Note(loaded.Value!, field.Value!, request.NoteId, logger);
        if (note.IsFailure) return note;

        field.Value!.Notes.Remove(note.Value!);
        return await Save(dataSets, loaded.Value!, logger, ct).ConfigureAwait(false);
    }

    /// <summary>Adds one intended source to an abstract Graph data set and answers it, with its stable id, author and time; a repeat of one already stated is refused by name.</summary>
    internal static async Task<IGenericResult<GraphIntendedSourceRequest>> AddIntendedSource(
        IDataSetConfigurationProvider dataSets, AddGraphIntendedSourceRequest request, GraphDataSetStamp stamp, ILogger logger, CancellationToken ct)
    {
        var loaded = await Load(dataSets, request.Name, logger, ct).ConfigureAwait(false);
        if (loaded.IsFailure) return loaded.ToNewResult<GraphIntendedSourceRequest>();

        var graph = loaded.Value!;
        if (graph.Components.Count > 0)
        {
            return GenericResult<GraphIntendedSourceRequest>.Failure(DataServiceResultCodes.ByName("GraphDataSetIntentOnBoundGraph"), logger, ResultDetails.Create("dataSet", graph.Name));
        }

        var stated = new GraphIntendedSourceRequest { Kind = request.Kind, DataStore = request.DataStore, DataSet = request.DataSet, Note = request.Note };
        var ordinal = graph.Intents.Count == 0 ? 0 : graph.Intents.Max(i => i.Ordinal) + 1;
        var made = await GraphDataSetDefinitions.Intent(graph.Name, stated, ordinal, graph.Intents, dataSets, stamp, logger, ct).ConfigureAwait(false);
        if (made.IsFailure) return made.ToNewResult<GraphIntendedSourceRequest>();

        var intent = made.Value!;
        if (graph.Intents.Any(i => Repeats(i, intent)))
        {
            return GenericResult<GraphIntendedSourceRequest>.Failure(
                DataServiceResultCodes.ByName("GraphDataSetIntendedSourceDuplicate"), logger,
                ResultDetails.Create("dataSet", graph.Name, "kind", intent.IntentKind, "name", (GraphIntentKinds.ByName(intent.IntentKind).NamesDataStore ? intent.DataStoreName : intent.SourceDataSetName) ?? string.Empty));
        }

        graph.Intents.Add(intent);
        var saved = await Save(dataSets, graph, logger, ct).ConfigureAwait(false);
        return saved.IsFailure ? saved.ToNewResult<GraphIntendedSourceRequest>() : GenericResult<GraphIntendedSourceRequest>.Success(GraphDataSetGraphPayloads.IntendedSource(intent));
    }

    /// <summary>Removes one intended source; the data set's other intended sources are left as they are.</summary>
    internal static async Task<IGenericResult> RemoveIntendedSource(
        IDataSetConfigurationProvider dataSets, GraphIntendedSourceRouteRequest request, ILogger logger, CancellationToken ct)
    {
        var loaded = await Load(dataSets, request.Name, logger, ct).ConfigureAwait(false);
        if (loaded.IsFailure) return loaded;

        var source = IntendedSource(loaded.Value!, request.SourceId, logger);
        if (source.IsFailure) return source;

        loaded.Value!.Intents.Remove(source.Value!);
        return await Save(dataSets, loaded.Value!, logger, ct).ConfigureAwait(false);
    }

    // The same kind naming the same data store or the same data set: what the row's unique index keeps to one.
    private static bool Repeats(GraphDataSetIntentConfiguration stored, GraphDataSetIntentConfiguration made)
        => string.Equals(stored.IntentKind, made.IntentKind, StringComparison.Ordinal)
            && string.Equals(stored.DataStoreName, made.DataStoreName, StringComparison.OrdinalIgnoreCase)
            && stored.SourceDataSetId == made.SourceDataSetId;
}
