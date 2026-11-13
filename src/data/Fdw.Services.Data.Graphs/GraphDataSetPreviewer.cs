using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Graphs;

/// <summary>
/// Reads the rows of a Graph data set draft that is not stored, with a cap on the rows each source gives and, when asked, at a component's output in the
/// middle of the graph (FDW-5454).
/// </summary>
/// <remarks>
/// <para>
/// The run is the runner's own (<see cref="GraphDataSetRunner"/>): the same engine, the same gateway for the caller, the same row and time limits, and a test run
/// that caps what each source gives. A component in the middle of the graph is read at by running only it and what feeds it, so a source nothing it needs
/// reads is not read.
/// </para>
/// <para>
/// Nothing is stored and no run record is kept. A run that cannot finish answers a named failure and never the rows read so far.
/// </para>
/// </remarks>
public sealed class GraphDataSetPreviewer : IGraphDataSetPreview
{
    private readonly GraphDataSetRunner _runner;
    private readonly GraphDataSetReadOptions _read;
    private readonly GraphDataSetPreviewOptions _options;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="GraphDataSetPreviewer"/> class.</summary>
    /// <param name="runner">The runner whose run a preview is.</param>
    /// <param name="read">The limits of a read, of which the link capacity is the preview's too.</param>
    /// <param name="options">The configured number of rows each source gives when the caller states none.</param>
    /// <param name="logger">The logger.</param>
    public GraphDataSetPreviewer(GraphDataSetRunner runner, GraphDataSetReadOptions read, GraphDataSetPreviewOptions options, ILogger<GraphDataSetPreviewer>? logger = null)
    {
        _runner = runner;
        _read = read;
        _options = options;
        _logger = logger ?? NullLogger<GraphDataSetPreviewer>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>> Preview(
        IDataGateway gateway, IGraphDataSetConfiguration draft, GraphPreviewRequest request, CancellationToken cancellationToken = default)
    {
        var limits = _runner.CheckLimits(draft.Name);
        if (limits.IsFailure)
        {
            return limits.ToNewResult<IReadOnlyList<IDictionary<string, object?>>>();
        }

        var cap = RowsPerSource(request);
        if (cap < 1)
        {
            return Failure("GraphDataSetPreviewCapInvalid", ResultDetails.Create("dataSet", draft.Name, "cap", cap));
        }

        var graph = GraphDataSetPipelines.Read(draft, _read.LinkCapacity);
        if (request.Component is null && request.Output is null)
        {
            return DraftEnd(draft) is { } own
                ? await _runner.RunGraph(gateway, draft.Name, graph, own, new GraphTestRun(cap, true), cancellationToken).ConfigureAwait(false)
                : _runner.NotBound(draft.Name);
        }

        var chosen = ChosenEnd(draft.Name, graph, request);
        return chosen.IsFailure || chosen.Value is null
            ? chosen.ToNewResult<IReadOnlyList<IDictionary<string, object?>>>()
            : await _runner.RunGraph(
                gateway, draft.Name, GraphDataSetPipelines.UpTo(graph, chosen.Value), chosen.Value, new GraphTestRun(cap, true), cancellationToken).ConfigureAwait(false);
    }

    // The caller's number when the caller states one, the configured number when not (stated in configuration, declared once on the service type).
    private int RowsPerSource(GraphPreviewRequest request)
    {
        if (request.MaxRowsPerSource is { } stated)
        {
            return stated;
        }

        return _options.RowsPerSource;
    }

    private static GraphEnd? DraftEnd(IGraphDataSetConfiguration draft) =>
        draft.Components.Count > 0 && draft.FieldsOutComponent is { Length: > 0 } component && draft.FieldsOutOutput is { Length: > 0 } output ? new GraphEnd(component, output) : null;

    // The component and output a caller asks to read at: both stated, the component in the draft and the output one of its own.
    private IGenericResult<GraphEnd> ChosenEnd(string dataSet, GraphPipelineConfiguration graph, GraphPreviewRequest request)
    {
        if (request.Component is not { Length: > 0 } component || request.Output is not { Length: > 0 } output)
        {
            return EndNotFound(dataSet, request, "a component and an output are stated together");
        }

        var found = graph.Components.FirstOrDefault(candidate => string.Equals(candidate.Name, component, StringComparison.OrdinalIgnoreCase));
        if (found is null)
        {
            return EndNotFound(dataSet, request, "the draft has no component of that name");
        }

        return ComponentShapes.Of(found, [.. graph.Links]).Outputs.Contains(output, StringComparer.OrdinalIgnoreCase)
            ? GenericResult<GraphEnd>.Success(new GraphEnd(found.Name, output))
            : EndNotFound(dataSet, request, "the component has no output of that name");
    }

    private IGenericResult<GraphEnd> EndNotFound(string dataSet, GraphPreviewRequest request, string reason) =>
        GenericResult<GraphEnd>.Failure(
            DataServiceResultCodes.ByName("GraphDataSetPreviewEndNotFound"), _logger,
            ResultDetails.Create("dataSet", dataSet, "component", request.Component ?? string.Empty, "output", request.Output ?? string.Empty).With("reason", reason));

    private IGenericResult<IReadOnlyList<IDictionary<string, object?>>> Failure(string code, ResultDetails details) =>
        GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Failure(DataServiceResultCodes.ByName(code), _logger, details);
}
