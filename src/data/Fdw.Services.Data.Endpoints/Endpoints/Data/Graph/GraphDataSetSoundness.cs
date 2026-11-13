using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Graphs;
using Fdw.Services.Data.Results;
using Fdw.Services.Etl.Graphs;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Whether a Graph data set is sound (FDW-5442): the one judgement the validate route and the routes that save a Graph data set both make, over the
/// data set as it would be stored.
/// </summary>
/// <remarks>
/// <para>
/// It is the check of the data set (<see cref="GraphDataSetCheck"/>), the engine's check of the graph's shape with the stated fields-out end consumed as the read consumes
/// it, the join keys, and each intended source judged as the provider judges it when the data set is written. What is not a problem is told apart: an intended source
/// not bound yet is information, and an abstract data set (no components) is sound.
/// </para>
/// <para>
/// Validate judges the draft a request states and save judges the configuration it built from the same request, so a body that validate calls sound is one save
/// accepts, and one that validate calls unsound is one save refuses.
/// </para>
/// </remarks>
internal static class GraphDataSetSoundness
{
    // The run settings of a graph pipeline the shape validator also judges. A data set graph is read, not run as a pipeline, and states none of them.
    private static readonly string[] PipelineRunSettings = ["linkCapacity", "maxParallelRuns", "overflowBehavior"];

    // The shape check needs a link capacity of at least one to leave that setting out of its problems; a data set states none and the check judges none.
    private const int ShapeCheckLinkCapacity = 1;

    private const string Components = "components";
    private const string IntendedSources = "intendedSources";

    /// <summary>Judges a Graph data set as it would be stored.</summary>
    /// <param name="draft">The data set: its fields out, graph and intended sources.</param>
    /// <param name="sources">Reads the data sets and data stores the data set reads or intends to read.</param>
    /// <param name="judgeShape">Whether the graph's shape is judged; false when the body states a graph that could not be read, which is one problem of its own.</param>
    /// <param name="logger">The logger each problem is written to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Every problem and every intended source not bound yet, or the failure of a read.</returns>
    internal static async Task<IGenericResult<GraphDataSetCheckResult>> Check(
        GraphDataSetConfiguration draft, IGraphDataSetSources sources, bool judgeShape, ILogger logger, CancellationToken cancellationToken = default)
    {
        var checkedDraft = await GraphDataSetCheck.Check(draft, sources, logger, cancellationToken).ConfigureAwait(false);
        if (checkedDraft.IsFailure || checkedDraft.Value is null)
        {
            return checkedDraft;
        }

        List<GraphDataSetProblem> problems = [.. checkedDraft.Value.Problems];
        List<GraphDataSetProblem> notes = [];
        if (judgeShape)
        {
            problems.AddRange(ShapeProblems(draft, logger));
            var fed = await GraphDataSetFedFields.Judge(draft, GraphDataSetPipelines.Read(draft, ShapeCheckLinkCapacity), sources, logger, cancellationToken).ConfigureAwait(false);
            if (fed.IsFailure)
            {
                return fed.ToNewResult<GraphDataSetCheckResult>();
            }

            problems.AddRange(fed.Value.Problems);
            notes.AddRange(fed.Value.Notes);
        }

        problems.AddRange(IntentProblems(draft, logger));
        return GenericResult<GraphDataSetCheckResult>.Success(new GraphDataSetCheckResult(problems, checkedDraft.Value.NotBound, notes));
    }

    /// <summary>Judges the data set a route is about to save: success when it is sound, or the refusal that lists each problem with its code and path.</summary>
    /// <param name="draft">The data set as it would be stored.</param>
    /// <param name="sources">Reads the data sets and data stores the data set reads or intends to read.</param>
    /// <param name="logger">The logger each problem and the refusal are written to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success, the refusal, or the failure of a read.</returns>
    internal static async Task<IGenericResult> Judge(
        GraphDataSetConfiguration draft, IGraphDataSetSources sources, ILogger logger, CancellationToken cancellationToken = default)
    {
        var judged = await Check(draft, sources, true, logger, cancellationToken).ConfigureAwait(false);
        if (judged.IsFailure || judged.Value is null)
        {
            return judged;
        }

        return judged.Value.Problems.Count == 0
            ? GenericResult.Success()
            : GenericResult.Failure(
                DataServiceResultCodes.ByName("GraphDataSetDraftNotSound"), logger,
                ResultDetails.Create("dataSet", draft.Name, "problems", string.Join("; ", judged.Value.Problems.Select(problem => $"{problem.Code} {problem.Message} ({string.Join('/', problem.Path)})"))));
    }

    /// <summary>Makes an engine problem of a graph a problem of the data set.</summary>
    /// <param name="dataSet">The data set's name.</param>
    /// <param name="problem">The engine's problem, with its path.</param>
    /// <param name="logger">The logger the problem is written to.</param>
    /// <returns>The problem, as <c>GraphDataSetComponentInvalid</c>.</returns>
    internal static GraphDataSetProblem Invalid(string dataSet, GraphProblem problem, ILogger logger) =>
        GraphDataSetProblems.Of("GraphDataSetComponentInvalid", logger, ResultDetails.Create("dataSet", dataSet, "reason", problem.Reason), [.. problem.Path]);

    /// <summary>Tells whether an engine problem is about a component of a kind a Graph data set refuses, which the check names with that kind's own code.</summary>
    /// <param name="problem">The engine's problem.</param>
    /// <param name="names">The names of the components of a refused kind.</param>
    /// <returns>Whether the problem concerns one of them.</returns>
    internal static bool ConcernsComponent(GraphProblem problem, HashSet<string> names) =>
        problem.Path.Count >= 2 && string.Equals(problem.Path[0], Components, StringComparison.Ordinal) && names.Contains(problem.Path[1]);

    // The engine's check of the graph the data set stores, as the read builds it, and the join keys. A component of a refused kind is the check's to name, so the
    // engine's problems for it are left out.
    private static IEnumerable<GraphDataSetProblem> ShapeProblems(GraphDataSetConfiguration draft, ILogger logger)
    {
        if (draft.Components.Count == 0)
        {
            yield break;
        }

        var graph = GraphDataSetPipelines.Read(draft, ShapeCheckLinkCapacity);
        var refused = new HashSet<string>(
            draft.Components.Where(component => GraphDataSetRefusals.FamilyOf(component.ComponentType) is not null).Select(component => component.Name),
            StringComparer.OrdinalIgnoreCase);
        var end = draft.FieldsOutComponent is { } component && draft.FieldsOutOutput is { } output ? new GraphEnd(component, output) : null;
        foreach (var problem in GraphShapeValidator.Problems(graph, end, logger).Where(found => !IsPipelineRunSetting(found) && !ConcernsComponent(found, refused)))
        {
            yield return Invalid(draft.Name, problem, logger);
        }

        foreach (var problem in GraphDataSetJoinKeys.Problems(graph, draft.Name, logger))
        {
            yield return problem;
        }
    }

    private static bool IsPipelineRunSetting(GraphProblem problem) =>
        problem.Path.Count == 1 && PipelineRunSettings.Contains(problem.Path[0], StringComparer.Ordinal);

    // The intents' own validator, run over each intent as the provider runs it when the data set is written. An intended data set that does not exist is named by
    // the check, which also states the id of the ones that do; an intent that names a missing data set has no id to judge and is left out here.
    private static IEnumerable<GraphDataSetProblem> IntentProblems(GraphDataSetConfiguration draft, ILogger logger)
    {
        var validator = new GraphDataSetIntentConfigurationValidator();
        for (var index = 0; index < draft.Intents.Count; index++)
        {
            var intent = draft.Intents[index];
            var namesMissingDataSet = GraphIntentKinds.ByName(intent.IntentKind).NamesDataSet && !string.IsNullOrEmpty(intent.SourceDataSetName) && intent.SourceDataSetId is null;
            if (namesMissingDataSet)
            {
                continue;
            }

            foreach (var error in validator.Validate(intent).Errors)
            {
                yield return GraphDataSetProblems.Of(
                    "GraphDataSetIntentInvalid", logger,
                    ResultDetails.Create("index", index, "dataSet", draft.Name, "reason", error.ErrorMessage),
                    IntendedSources, index.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
