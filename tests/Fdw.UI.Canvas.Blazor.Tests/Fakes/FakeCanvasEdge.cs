using System.Diagnostics.CodeAnalysis;
using Fdw.UI.Abstractions.Canvas;

namespace Fdw.UI.Canvas.Blazor.Tests.Fakes;

/// <summary>
/// Simple mutable canvas edge for testing.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class FakeCanvasEdge : ICanvasEdge
{
    /// <inheritdoc />
    public string Id { get; set; } = "fake-edge";

    /// <inheritdoc />
    public string SourceNodeId { get; set; } = "node-1";

    /// <inheritdoc />
    public string TargetNodeId { get; set; } = "node-2";

    /// <inheritdoc />
    public string? SourcePortId { get; set; }

    /// <inheritdoc />
    public string? TargetPortId { get; set; }

    /// <inheritdoc />
    public ICanvasEdgeType EdgeType { get; set; } = CanvasEdgeTypes.ByName("Flow");

    /// <inheritdoc />
    public string? Label { get; set; }
}
