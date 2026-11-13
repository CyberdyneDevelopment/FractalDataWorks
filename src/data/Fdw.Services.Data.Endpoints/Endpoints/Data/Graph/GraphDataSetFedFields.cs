using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Describing;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Judges that the field each field out is fed by is a field of the output that carries the fields out (FDW-5522), from the fields the describer derives.
/// </summary>
/// <remarks>
/// <para>
/// The describer that answers the describe routes derives the fields of every node of the graph from the data sets and the components over them, so the check asks it and
/// is not a second reader of sources: the data sets' stored fields come through <see cref="IGraphDataSetSources.Fields"/>, read once for every data set the graph names.
/// </para>
/// <para>
/// A field the output does not have is a problem (<c>GraphDataSetFedFieldMissing</c>). When the output's fields are not known (the describer derives none for a container or
/// SQL source, or for a kind it has no deriver for, or the graph cannot be described) nothing can be judged, and the field is reported as a note that it was not checked
/// (<c>GraphDataSetFedFieldNotChecked</c>), never as a problem: a note does not block a save and save does not read it.
/// </para>
/// </remarks>
internal static class GraphDataSetFedFields
{
    private const string FieldsOut = "fieldsOut";

    /// <summary>Judges the fields out that state what feeds them.</summary>
    /// <param name="draft">The draft, with the component and output that carry its fields out.</param>
    /// <param name="graph">The draft's graph as the engine reads it.</param>
    /// <param name="sources">Reads the stored fields of the data sets the graph names.</param>
    /// <param name="logger">The logger each problem and note is written to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The problems and the notes, or the failure of a read.</returns>
    internal static Task<IGenericResult<(IReadOnlyList<GraphDataSetProblem> Problems, IReadOnlyList<GraphDataSetProblem> Notes)>> Judge(
        GraphDataSetConfiguration draft, GraphPipelineConfiguration graph, IGraphDataSetSources sources, ILogger logger, CancellationToken cancellationToken = default) =>
        Judge(draft, graph, sources, GraphDescriber.Describe, logger, cancellationToken);

    /// <summary>Judges the fields out that state what feeds them, with the describer given: the test of a describer that fails names its own.</summary>
    /// <param name="draft">The draft, with the component and output that carry its fields out.</param>
    /// <param name="graph">The draft's graph as the engine reads it.</param>
    /// <param name="sources">Reads the stored fields of the data sets the graph names.</param>
    /// <param name="describe">Describes the graph: <see cref="GraphDescriber.Describe"/> everywhere but a test.</param>
    /// <param name="logger">The logger each problem and note is written to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The problems and the notes, or the failure of a read.</returns>
    internal static async Task<IGenericResult<(IReadOnlyList<GraphDataSetProblem> Problems, IReadOnlyList<GraphDataSetProblem> Notes)>> Judge(
        GraphDataSetConfiguration draft,
        GraphPipelineConfiguration graph,
        IGraphDataSetSources sources,
        Func<GraphPipelineConfiguration, DescribeContext, ILogger, CancellationToken, Task<IGenericResult<IReadOnlyList<GraphNodeFields>>>> describe,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var fed = FedFields(draft);
        if (fed.Count == 0)
        {
            return Answer([], []);
        }

        var read = await sources.Fields(
            [.. graph.Components.Select(component => component.DataSetName).Where(name => !string.IsNullOrEmpty(name)).Select(name => name!).Distinct(StringComparer.OrdinalIgnoreCase)],
            cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is null)
        {
            return read.ToNewResult<(IReadOnlyList<GraphDataSetProblem>, IReadOnlyList<GraphDataSetProblem>)>();
        }

        var context = new DescribeContext(
            (name, _) => Task.FromResult<IGenericResult<IReadOnlyList<GraphField>>>(
                GenericResult<IReadOnlyList<GraphField>>.Success(read.Value.TryGetValue(name, out var fields) ? [.. fields.Select(field => new GraphField(field.Name, field.TypeName))] : default!)),
            (request, token) => GraphContainerFields.Read(sources, request, logger, token));
        var described = await describe(graph, context, logger, cancellationToken).ConfigureAwait(false);
        var output = described.IsSuccess
            ? described.Value?.FirstOrDefault(node => !node.IsInput && Same(node.Component, draft.FieldsOutComponent) && Same(node.Node, draft.FieldsOutOutput))?.Fields
            : null;

        // Why the reason is stated: a describer that FAILED must not look like an output whose fields are unknown by design. The failure is already logged by the describer that made it,
        // and its own message goes into the note.
        var reason = described.IsFailure ? $"the describer failed: {described.CurrentMessage ?? string.Empty}" : "the describer derives no fields for this output";
        return Answer(
            [.. fed.Where(field => output is not null && !output.Any(known => Same(known.Name, field.FedByField))).Select(field => Missing(draft, field, logger))],
            [.. fed.Where(field => output is null).Select(field => NotChecked(draft, field, reason, logger))]);
    }

    // The fields out that state what feeds them, from the component and output that carry the fields out; a field fed from anywhere else is judged by the check (FedByElsewhere).
    private static List<GraphDataSetFieldConfiguration> FedFields(GraphDataSetConfiguration draft) =>
        string.IsNullOrEmpty(draft.FieldsOutComponent) || string.IsNullOrEmpty(draft.FieldsOutOutput) || draft.Components.Count == 0
            ? []
            : [.. draft.GraphFields.Where(field => !string.IsNullOrEmpty(field.FedByField) && Same(field.FedByComponent, draft.FieldsOutComponent))];

    private static GraphDataSetProblem Missing(GraphDataSetConfiguration draft, GraphDataSetFieldConfiguration field, ILogger logger) =>
        GraphDataSetProblems.Of("GraphDataSetFedFieldMissing", logger, Details(draft, field), FieldsOut, field.Name);

    private static GraphDataSetProblem NotChecked(GraphDataSetConfiguration draft, GraphDataSetFieldConfiguration field, string reason, ILogger logger) =>
        GraphDataSetProblems.Of("GraphDataSetFedFieldNotChecked", logger, Details(draft, field).With("reason", reason), FieldsOut, field.Name);

    private static ResultDetails Details(GraphDataSetConfiguration draft, GraphDataSetFieldConfiguration field) =>
        ResultDetails.Create("field", field.Name, "dataSet", draft.Name, "feedField", field.FedByField).With("component", draft.FieldsOutComponent).With("output", draft.FieldsOutOutput);

    private static bool Same(string? left, string? right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static IGenericResult<(IReadOnlyList<GraphDataSetProblem> Problems, IReadOnlyList<GraphDataSetProblem> Notes)> Answer(
        IReadOnlyList<GraphDataSetProblem> problems, IReadOnlyList<GraphDataSetProblem> notes) =>
        GenericResult<(IReadOnlyList<GraphDataSetProblem> Problems, IReadOnlyList<GraphDataSetProblem> Notes)>.Success((problems, notes));
}
