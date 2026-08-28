using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.UI.Abstractions.Canvas;
using Fdw.UI.Abstractions.RenderModeOptions;
using Moq;

namespace Fdw.UI.Canvas.Blazor.Tests.Fakes;

/// <summary>
/// Simple mutable canvas model for testing. Contains a small graph: 2 nodes + 1 edge.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class FakeCanvasModel : ICanvasModel
{
    private static readonly IRenderMode _viewMode = CreateViewMode();

    private static IRenderMode CreateViewMode()
    {
        var mock = new Mock<IRenderMode>();
        mock.Setup(m => m.Name).Returns("View");
        mock.Setup(m => m.AllowsEditing).Returns(false);
        mock.Setup(m => m.ShowsView).Returns(true);
        return mock.Object;
    }

    /// <inheritdoc />
    public string Id { get; set; } = "test-canvas";

    /// <inheritdoc />
    public string Title { get; set; } = "Test Canvas";

    /// <inheritdoc />
    public IRenderMode RenderMode { get; set; } = _viewMode;

    /// <inheritdoc />
    public IReadOnlyList<ICanvasNode> Nodes { get; set; } = BuildDefaultNodes();

    /// <inheritdoc />
    public IReadOnlyList<ICanvasEdge> Edges { get; set; } = BuildDefaultEdges();

    /// <inheritdoc />
    public string? LayoutHint { get; set; }

    /// <inheritdoc />
    public string? SelectedId { get; set; }

    /// <inheritdoc />
    public ICanvasEditContext? EditContext { get; set; }

    // ── Helpers ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds two default nodes with the Pipeline node type.
    /// </summary>
    internal static IReadOnlyList<ICanvasNode> BuildDefaultNodes() =>
    [
        new FakeCanvasNode
        {
            Id = "node-1",
            Label = "Source",
            NodeType = CanvasNodeTypes.ByName("Pipeline"),
            X = 100,
            Y = 150,
        },
        new FakeCanvasNode
        {
            Id = "node-2",
            Label = "Target",
            NodeType = CanvasNodeTypes.ByName("DataSet"),
            X = 400,
            Y = 150,
        },
    ];

    /// <summary>
    /// Builds one default Flow edge connecting node-1 to node-2.
    /// </summary>
    internal static IReadOnlyList<ICanvasEdge> BuildDefaultEdges() =>
    [
        new FakeCanvasEdge
        {
            Id = "edge-1",
            SourceNodeId = "node-1",
            TargetNodeId = "node-2",
            EdgeType = CanvasEdgeTypes.ByName("Flow"),
        },
    ];
}
