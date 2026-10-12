namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// Lightweight representation of a visualization type for API responses.
/// </summary>
public sealed class VisualizationTypeItem
{
    /// <summary>
    /// Gets or sets the visualization type name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon identifier.
    /// </summary>
    public string Icon { get; set; } = string.Empty;
}
