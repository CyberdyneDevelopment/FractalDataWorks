using System.Collections.Generic;
using Fdw.UI.Abstractions.Canvas;
using Fdw.UI.Abstractions.RenderModeOptions;

namespace Fdw.Services.Pipelines.Components.Canvas;

/// <summary>
/// Free-graph, in-memory canvas model for a single ETL pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Node roles:
/// <list type="bullet">
/// <item>One <c>DataSet</c> node with <c>DataSetRole = "Source"</c> for the pipeline source.</item>
/// <item>One <c>DataSet</c> node with <c>DataSetRole = "Sink"</c> for the pipeline destination.</item>
/// <item>Zero or more <c>Transform</c> nodes, one per <c>PipelineTransformConfiguration</c>.</item>
/// </list>
/// </para>
/// <para>
/// Edges are all <c>Flow</c> edges forming the execution chain: source → (transforms in
/// <c>ExecutionOrder</c> order) → sink. The model allows free placement — validity is checked by
/// <see cref="Validation.PipelineGraphValidator"/>, not enforced structurally here.
/// </para>
/// <para>
/// This is a DRAFT editor model. It does NOT persist; persistence is the provider Save
/// (wired in a later phase).
/// </para>
/// </remarks>
public sealed class PipelineCanvasModel : ICanvasModel
{
    private readonly List<PipelineCanvasNode> _nodes;
    private readonly List<PipelineCanvasEdge> _edges;
    private PipelineCanvasEditContext? _editContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineCanvasModel"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this canvas instance.</param>
    /// <param name="title">The display title shown in the canvas chrome.</param>
    /// <param name="renderMode">The current render mode (View or Edit).</param>
    /// <param name="nodes">The initial set of nodes.</param>
    /// <param name="edges">The initial set of edges.</param>
    /// <param name="selectedId">The optional initially selected node or edge identifier.</param>
    /// <param name="pipelineType">
    /// The optional engine discriminator (e.g. "BatchCopy") for this pipeline. Null means "not yet
    /// known" (a canvas being built from scratch, before the toolbar engine picker or a load has set
    /// it) — <see cref="Projection.PipelineCreateRequestProjection.ToCreateRequest"/> fails loud if
    /// it is still null/empty when a create-pipeline request is projected.
    /// </param>
    public PipelineCanvasModel(
        string id,
        string title,
        IRenderMode renderMode,
        IEnumerable<PipelineCanvasNode>? nodes = null,
        IEnumerable<PipelineCanvasEdge>? edges = null,
        string? selectedId = null,
        string? pipelineType = null)
    {
        Id = id;
        Title = title;
        RenderMode = renderMode;
        _nodes = nodes is null ? [] : [..nodes];
        _edges = edges is null ? [] : [..edges];
        SelectedId = selectedId;
        PipelineType = pipelineType;

        // Why: wire the edit context to this model immediately so consumers don't have to null-check
        // when RenderMode allows editing. The context holds a back-reference to mutate _nodes/_edges.
        if (renderMode.AllowsEditing)
            _editContext = new PipelineCanvasEditContext(this);
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Title { get; }

    /// <inheritdoc />
    public IRenderMode RenderMode { get; }

    /// <inheritdoc />
    public IReadOnlyList<ICanvasNode> Nodes => _nodes;

    /// <inheritdoc />
    public IReadOnlyList<ICanvasEdge> Edges => _edges;

    /// <inheritdoc />
    public string? LayoutHint => null;

    /// <inheritdoc />
    public string? SelectedId { get; internal set; }

    /// <summary>
    /// Gets the engine discriminator (e.g. "BatchCopy") for this pipeline, or null if not yet known.
    /// </summary>
    /// <remarks>
    /// Concrete-only — not part of <see cref="ICanvasModel"/>. Set at construction (from a loaded
    /// <c>PipelineDetailResponse.PipelineType</c>) or later via a canvas toolbar engine picker.
    /// </remarks>
    public string? PipelineType { get; internal set; }

    /// <inheritdoc />
    public ICanvasEditContext? EditContext => _editContext;

    // ── Internal mutation surface (used by PipelineCanvasEditContext) ─────────

    /// <summary>Gets the mutable node list for use by <see cref="PipelineCanvasEditContext"/>.</summary>
    internal List<PipelineCanvasNode> MutableNodes => _nodes;

    /// <summary>Gets the mutable edge list for use by <see cref="PipelineCanvasEditContext"/>.</summary>
    internal List<PipelineCanvasEdge> MutableEdges => _edges;
}
