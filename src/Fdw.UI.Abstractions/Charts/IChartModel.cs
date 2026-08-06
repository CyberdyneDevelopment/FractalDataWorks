using System.Collections.Generic;
using Fdw.UI.Abstractions.RenderModeOptions;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// The render-agnostic specification for a chart tile.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IChartModel"/> mirrors the shape of the page-model family (Id, Title, RenderMode)
/// for consistency with the FDW page-model family. The same chart model is used regardless of
/// whether the host renders it with a Blazor chart component, an SVG export, or a TUI sparkline —
/// the <see cref="ChartType"/> and <see cref="Encodings"/> carry the data-binding contract;
/// the renderer decides how to fulfil it.
/// </para>
/// <para>
/// No Blazor, ASP.NET, or chart-library types appear in this interface — the chart contract layer
/// is render-agnostic.
/// </para>
/// </remarks>
public interface IChartModel
{
    /// <summary>
    /// Gets the unique identifier for this chart tile instance.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display title shown in the chart chrome.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the current render mode (View or Edit).
    /// </summary>
    /// <remarks>
    /// Reuses the existing <see cref="IRenderMode"/> TypeCollection — no chart-specific mode enum.
    /// Renderers check <see cref="IRenderMode.AllowsEditing"/> to decide whether to surface
    /// chart configuration controls.
    /// </remarks>
    IRenderMode RenderMode { get; }

    /// <summary>
    /// Gets the chart type that determines the visual form and encoding requirements.
    /// </summary>
    IChartType ChartType { get; }

    /// <summary>
    /// Gets the data source descriptor identifying the dataset and optional query parameters.
    /// </summary>
    IChartDataSource DataSource { get; }

    /// <summary>
    /// Gets the field-to-role bindings for this chart.
    /// </summary>
    /// <remarks>
    /// Each entry binds one <see cref="IChartEncodingRole"/> to a named data field.
    /// Roles absent from this collection are unbound — renderers must tolerate missing
    /// optional encodings. Hosts validate that all <see cref="IChartType.RequiredEncodings"/>
    /// are present before invoking the renderer.
    /// </remarks>
    IReadOnlyList<ChartEncoding> Encodings { get; }

    /// <summary>
    /// Gets an optional subtitle or description shown beneath the title.
    /// </summary>
    string? Subtitle { get; }

    /// <summary>
    /// Gets a value indicating whether the X axis label is shown.
    /// </summary>
    bool ShowXAxisLabel { get; }

    /// <summary>
    /// Gets a value indicating whether the Y axis label is shown.
    /// </summary>
    bool ShowYAxisLabel { get; }

    /// <summary>
    /// Gets a value indicating whether the chart legend is shown.
    /// </summary>
    bool ShowLegend { get; }

    /// <summary>
    /// Gets a value indicating whether hover tooltips are enabled.
    /// </summary>
    bool EnableTooltips { get; }

    /// <summary>
    /// Gets a value indicating whether axis zoom / pan interaction is enabled.
    /// </summary>
    bool EnableZoom { get; }

    /// <summary>
    /// Gets the renderer hints escape-bag for chart-library-specific extras.
    /// </summary>
    /// <remarks>
    /// Keys and values are agreed by convention between the domain provider that builds the
    /// <see cref="IChartModel"/> and the renderer implementation. The chart contract layer
    /// does not interpret them. Examples: <c>"theme"</c>, <c>"palette"</c>,
    /// <c>"stacked"</c>, <c>"curve"</c>.
    /// A null value is treated identically to an empty dictionary — no hints.
    /// </remarks>
    IReadOnlyDictionary<string, string>? RendererHints { get; }
}
