using System.Collections.Generic;
using Fdw.UI.Abstractions.Charts;
using Fdw.UI.Abstractions.RenderModeOptions;

namespace Fdw.Data.Components.Visualization;

/// <summary>
/// Concrete mutable <see cref="IChartModel"/> implementation for the Visualize page.
/// Carries the full chart specification assembled by the headless provider from UI selections.
/// </summary>
/// <remarks>
/// Instances are rebuilt whenever the user changes dataset, chart type, or encoding mappings.
/// All properties use <c>{ get; set; }</c> so the Visualize page provider can update them
/// in place before passing them to <c>ChartHost</c>.
/// </remarks>
public sealed class ChartModel : IChartModel
{
    /// <summary>Gets or sets the unique identifier for this chart tile instance.</summary>
    public string Id { get; set; } = "visualize";

    /// <summary>Gets or sets the display title shown in the chart chrome.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the current render mode (View or Edit).</summary>
    public IRenderMode RenderMode { get; set; } = RenderModes.ByName("View");

    /// <summary>Gets or sets the chart type that determines the visual form and encoding requirements.</summary>
    public IChartType ChartType { get; set; } = ChartTypes.NotFound;

    /// <summary>Gets or sets the data source descriptor identifying the dataset.</summary>
    public IChartDataSource DataSource { get; set; } = ChartDataSource.Empty;

    /// <summary>Gets or sets the field-to-role bindings for this chart.</summary>
    public IReadOnlyList<ChartEncoding> Encodings { get; set; } = [];

    /// <summary>Gets or sets an optional subtitle or description shown beneath the title.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets a value indicating whether the X axis label is shown.</summary>
    public bool ShowXAxisLabel { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the Y axis label is shown.</summary>
    public bool ShowYAxisLabel { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the chart legend is shown.</summary>
    public bool ShowLegend { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether hover tooltips are enabled.</summary>
    public bool EnableTooltips { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether axis zoom/pan interaction is enabled.</summary>
    public bool EnableZoom { get; set; }

    /// <summary>Gets or sets the renderer hints escape-bag for chart-library-specific extras.</summary>
    public IReadOnlyDictionary<string, string>? RendererHints { get; set; }
}
