namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// A connection point on a canvas node through which edges attach.
/// </summary>
/// <remarks>
/// Ports give edges a precise attachment point, enabling multiple in/out connections on a single
/// node. The <see cref="Direction"/> is a TypeCollection option so that downstream assemblies can
/// extend it (e.g. adding a "Bidirectional" direction) without touching this contract.
/// </remarks>
public interface ICanvasPort
{
    /// <summary>
    /// Gets the unique identifier for this port within the owning node.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name for this port (e.g. "Input", "Output", "Error").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the direction of this port (In or Out).
    /// </summary>
    IPortDirection Direction { get; }

    /// <summary>
    /// Gets the optional data-type label for the port (e.g. "string", "IEnumerable&lt;Row&gt;").
    /// </summary>
    /// <remarks>
    /// This is a display string for the renderer — it is not interpreted by the canvas layer.
    /// Null means no data type label is shown.
    /// </remarks>
    string? DataType { get; }
}
