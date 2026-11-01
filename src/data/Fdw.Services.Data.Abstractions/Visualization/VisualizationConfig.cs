using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// Configuration settings for a visualization instance.
/// </summary>
public sealed class VisualizationConfig
{
    /// <summary>
    /// Gets or sets the title of the visualization.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of data points to display.
    /// </summary>
    public int MaxDataPoints { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether to show a legend.
    /// </summary>
    public bool ShowLegend { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show axis labels.
    /// </summary>
    public bool ShowAxisLabels { get; set; } = true;

    /// <summary>
    /// Gets or sets additional configuration properties.
    /// </summary>
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>(System.StringComparer.Ordinal);
}
