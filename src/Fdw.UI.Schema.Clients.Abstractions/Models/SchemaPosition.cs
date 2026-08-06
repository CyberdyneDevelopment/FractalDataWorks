namespace Fdw.UI.Schema.Clients.Models;

/// <summary>
/// Represents the visual position and dimensions of an entity in a schema diagram.
/// </summary>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class SchemaPosition
{
    /// <summary>
    /// Gets or sets the horizontal position (in diagram units).
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Gets or sets the vertical position (in diagram units).
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Gets or sets the optional width of the entity card (in diagram units).
    /// </summary>
    public double? Width { get; set; }

    /// <summary>
    /// Gets or sets the optional height of the entity card (in diagram units).
    /// </summary>
    public double? Height { get; set; }
}
