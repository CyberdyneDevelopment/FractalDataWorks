using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.UI.Abstractions.Canvas;

namespace Fdw.UI.Canvas.Blazor.Tests.Fakes;

/// <summary>
/// Simple mutable canvas node for testing.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class FakeCanvasNode : ICanvasNode
{
    /// <inheritdoc />
    public string Id { get; set; } = "fake-node";

    /// <inheritdoc />
    public ICanvasNodeType NodeType { get; set; } = CanvasNodeTypes.ByName("Pipeline");

    /// <inheritdoc />
    public string Label { get; set; } = "Fake Node";

    /// <inheritdoc />
    public string? SubLabel { get; set; }

    /// <inheritdoc />
    public string? Status { get; set; }

    /// <inheritdoc />
    public double X { get; set; }

    /// <inheritdoc />
    public double Y { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<ICanvasPort> Ports { get; set; } = [];

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(System.StringComparer.Ordinal);
}
