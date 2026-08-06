using System.Diagnostics.CodeAnalysis;
using Fdw.UI.Abstractions.Canvas;

namespace Fdw.Services.Pipelines.Components.Canvas;

/// <summary>
/// A connection port on a pipeline canvas node.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PipelineCanvasPort : ICanvasPort
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineCanvasPort"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this port within the owning node.</param>
    /// <param name="name">The display name for this port.</param>
    /// <param name="direction">The direction of this port (In or Out).</param>
    /// <param name="dataType">The optional data-type label for the port.</param>
    public PipelineCanvasPort(string id, string name, IPortDirection direction, string? dataType = null)
    {
        Id = id;
        Name = name;
        Direction = direction;
        DataType = dataType;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IPortDirection Direction { get; }

    /// <inheritdoc />
    public string? DataType { get; }
}
