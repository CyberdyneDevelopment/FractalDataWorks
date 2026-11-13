using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Checks a Graph data set draft without saving it (FDW-5290): the request of POST /datasets/graph is read the way saving reads it, and every
/// problem saving would be refused for, and every problem the engine's own check of the graph finds, is listed with its path.
/// </summary>
/// <remarks>
/// <para>
/// It runs the steps the create route runs, one field and one component at a time where the create route stops at the first, so each problem is
/// reported once and the answer is a list. What needs only the stored body (the refused kinds, what feeds a field out, the sources, the cycle) is
/// <see cref="GraphDataSetCheck"/>; what the engine can say about the components and links is the graph pipeline's <see cref="GraphShapeValidator"/>
/// over the draft read by the graph pipeline's own reader. A draft with no components is abstract and is not judged for a shape.
/// </para>
/// <para>
/// It judges no name, so one route serves a new data set and an edit of a saved one; an edit is checked by sending the whole draft.
/// </para>
/// </remarks>
internal static class GraphDataSetValidator
{
    private const string FieldsOut = "fieldsOut";
    private const string Components = "components";

    /// <summary>Checks the draft.</summary>
    /// <param name="request">The body of POST /datasets/graph.</param>
    /// <param name="sources">Reads the data sets and data stores the draft reads or intends to read.</param>
    /// <param name="logger">The logger each problem is written to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Every problem and every intended source not bound yet, or the failure of a read.</returns>
    internal static async Task<IGenericResult<GraphDataSetValidationPayload>> Validate(
        CreateGraphDataSetRequest request, IGraphDataSetSources sources, ILogger logger, CancellationToken cancellationToken = default)
    {
        var checkedDraft = await Draft(request, sources, logger, cancellationToken).ConfigureAwait(false);
        return checkedDraft.IsFailure || checkedDraft.Value is null
            ? checkedDraft.ToNewResult<GraphDataSetValidationPayload>()
            : GenericResult<GraphDataSetValidationPayload>.Success(checkedDraft.Value.Payload);
    }

    /// <summary>Checks the draft and answers it with what was found, so a route that reads the draft judges the same draft (FDW-5454).</summary>
    /// <param name="request">The body of POST /datasets/graph.</param>
    /// <param name="sources">Reads the data sets and data stores the draft reads or intends to read.</param>
    /// <param name="logger">The logger each problem is written to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The draft and its problems, or the failure of a read.</returns>
    internal static async Task<IGenericResult<GraphDataSetDraftCheck>> Draft(
        CreateGraphDataSetRequest request, IGraphDataSetSources sources, ILogger logger, CancellationToken cancellationToken = default)
    {
        var draft = new GraphDataSetConfiguration { Name = request.Name };
        var problems = new List<GraphDataSetProblem>();
        problems.AddRange(Header(request, logger));
        problems.AddRange(FieldsOutProblems(request, draft, logger));
        var graph = Graph(request, draft, logger);
        problems.AddRange(graph.Problems);
        problems.AddRange(Terminal(request, draft, logger));
        draft.Intents = [.. request.IntendedSources.Select((source, index) => new GraphDataSetIntentConfiguration
        {
            IntentKind = source.Kind, DataStoreName = source.DataStore, SourceDataSetName = source.DataSet, Note = source.Note, Ordinal = index,
        })];

        var checkedDraft = await GraphDataSetSoundness.Check(draft, sources, graph.Read, logger, cancellationToken).ConfigureAwait(false);
        if (checkedDraft.IsFailure || checkedDraft.Value is null)
        {
            return checkedDraft.ToNewResult<GraphDataSetDraftCheck>();
        }

        problems.AddRange(checkedDraft.Value.Problems);
        return GenericResult<GraphDataSetDraftCheck>.Success(new GraphDataSetDraftCheck(draft, new GraphDataSetValidationPayload
        {
            IsValid = problems.Count == 0,
            Problems = [.. problems.Select(problem => new GraphDataSetProblemPayload { Code = problem.Code, Message = problem.Message, Path = [.. problem.Path] })],
            NotBound = [.. checkedDraft.Value.NotBound.Select(source => new GraphDataSetNotBoundPayload
            {
                Path = [.. source.Path], Kind = source.IntentKind, Name = source.Name, Exists = source.Exists,
            })],
            NotChecked = [.. checkedDraft.Value.Notes.Select(note => new GraphDataSetProblemPayload { Code = note.Code, Message = note.Message, Path = [.. note.Path] })],
        }));
    }

    // The data set's own classification, judged as saving judges it.
    private static IEnumerable<GraphDataSetProblem> Header(CreateGraphDataSetRequest request, ILogger logger)
    {
        var classified = DataClassificationValidator.Validate([request.ClassificationId], logger);
        if (classified.IsFailure)
        {
            yield return GraphDataSetProblems.Of(classified, "classificationId");
        }
    }

    // Each field out judged alone by the steps saving runs, so every field with a problem is named. What feeds the field is the check's to judge (it names
    // half a feeds-from and a missing component with a path of their own), so it is copied to the draft and left out of the field the step sees.
    private static IEnumerable<GraphDataSetProblem> FieldsOutProblems(CreateGraphDataSetRequest request, GraphDataSetConfiguration draft, ILogger logger)
    {
        var values = new DataSetRequestValues(request.Name, logger);
        foreach (var field in request.FieldsOut)
        {
            draft.GraphFields.Add(new GraphDataSetFieldConfiguration { Name = field.Name, FedByComponent = field.FedByComponent, FedByField = field.FedByField });
            field.FedByComponent = null;
            field.FedByField = null;
            List<GraphFieldOutRequest> alone = [field];
            var valid = GraphDataSetDefinitions.ValidateFields([field.ClassificationId], alone, logger);
            var stated = valid.IsFailure ? valid : GraphDataSetDefinitions.Fields(new GraphDataSetConfiguration { Name = request.Name }, alone, values, GraphDataSetStamp.Checking, logger);
            if (stated.IsFailure)
            {
                yield return GraphDataSetProblems.Of(stated, FieldsOut, field.Name);
            }
        }
    }

    // Which component output carries the fields out, as saving judges it over the fields and the graph: stated whole, and every field out fed by that component.
    private static IEnumerable<GraphDataSetProblem> Terminal(CreateGraphDataSetRequest request, GraphDataSetConfiguration draft, ILogger logger)
    {
        var stated = GraphDataSetDefinitions.FieldsOut(draft, request.FieldsOutComponent, request.FieldsOutOutput, logger);
        if (stated.IsFailure)
        {
            yield return GraphDataSetProblems.Of(stated, "fieldsOutComponent");
        }
    }

    // The graph, read by the graph pipeline's own reader as the create route reads it, then judged: the reader's problems and the settings a data set graph does
    // not take. What is judged of the data set as stored (the shape and the join keys) is GraphDataSetSoundness's, so saving judges the same. The reader's problems for a component of a refused kind are left out, as the check names it.
    private static (List<GraphDataSetProblem> Problems, bool Read) Graph(CreateGraphDataSetRequest request, GraphDataSetConfiguration draft, ILogger logger)
    {
        var refused = new HashSet<string>(
            request.Components.Where(component => GraphDataSetRefusals.FamilyOf(component.Kind) is not null).Select(component => component.Name),
            StringComparer.OrdinalIgnoreCase);
        var unread = new List<GraphProblem>();
        var read = GraphPayloadReader.ReadDraft(new GraphPayload { Name = request.Name, Components = request.Components, Links = request.Links }, Guid.CreateVersion7(), unread);
        var found = unread.Where(problem => !GraphDataSetSoundness.ConcernsComponent(problem, refused)).Select(problem => GraphDataSetSoundness.Invalid(request.Name, problem, logger)).ToList();
        if (read.IsFailure || read.Value is null)
        {
            // A body the reader cannot read is one problem with no path; what the check can still judge of the components is their names and kinds.
            draft.Components = [.. request.Components.Select(component => new GraphDataSetComponentConfiguration { Name = component.Name, ComponentType = component.Kind })];
            found.Add(GraphDataSetProblems.Of(read));
            return (found, false);
        }

        draft.Components = [.. read.Value.Components.Select(GraphDataSetComponents.Read)];
        draft.Links = [.. read.Value.Links.Select(link => new GraphDataSetLinkConfiguration
        {
            Id = link.Id, FromComponent = link.FromComponent, FromOutput = link.FromOutput, ToComponent = link.ToComponent, ToInput = link.ToInput,
        })];
        draft.FieldsOutComponent = request.FieldsOutComponent;
        draft.FieldsOutOutput = request.FieldsOutOutput;
        found.AddRange(read.Value.Components.SelectMany(component => GraphDataSetComponents.UnsupportedSettings(component).Select(setting => GraphDataSetProblems.Of(
            "GraphDataSetSettingNotSupported", logger,
            ResultDetails.Create("component", component.Name, "dataSet", request.Name, "setting", setting),
            Components, component.Name, "settings", setting))));
        return (found, true);
    }
}
