using System;
using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// Interface for chart renderer type options.
/// </summary>
/// <remarks>
/// <para>
/// Each registered renderer type carries capability flags, the set of chart types it can draw,
/// and the CLR type of the UI component that hosts it. The type option is the descriptor; the
/// actual renderer instance is obtained from DI or a factory — it is not part of the
/// TypeCollection entry.
/// </para>
/// <para>
/// The <see cref="ChartRendererTypes"/> TypeCollection enables two host operations without
/// reflection or a separate map:
/// <list type="number">
/// <item>Populate a renderer-selection dropdown: <c>ChartRendererTypes.All()</c></item>
/// <item>Filter the chart-type dropdown to the selected renderer: check
///     <see cref="SupportedChartTypes"/> against <see cref="ChartTypes.All()"/>.</item>
/// </list>
/// </para>
/// <para>
/// Compare against <see cref="ChartRendererTypes.NotFound"/> — never null.
/// </para>
/// </remarks>
public interface IChartRendererType : ITypeOption<int, ChartRendererTypeBase>
{
    /// <summary>
    /// Gets the human-readable display name shown in the renderer-selection dropdown.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets a value indicating whether this renderer supports interactive user interactions
    /// (hover tooltips, zoom/pan, click events).
    /// </summary>
    bool SupportsInteraction { get; }

    /// <summary>
    /// Gets a value indicating whether this renderer stays performant on large series
    /// (thousands of data points without significant degradation).
    /// </summary>
    bool SupportsLargeSeries { get; }

    /// <summary>
    /// Gets a value indicating whether this renderer supports edit-mode configuration controls.
    /// </summary>
    bool SupportsEditing { get; }

    /// <summary>
    /// Gets the <see cref="ChartTypes"/> registry names of the chart types this renderer can draw.
    /// </summary>
    /// <remarks>
    /// A host uses this list to filter <see cref="ChartTypes.All()"/> down to the chart types
    /// compatible with the selected renderer — no reflection, no switch. An empty list means
    /// the renderer supports all registered chart types (treat as wildcard).
    /// </remarks>
    IReadOnlyList<string> SupportedChartTypes { get; }

    /// <summary>
    /// Gets the CLR type of the UI component that renders this chart, or <see langword="null"/>
    /// for renderers that have no component-tree representation (e.g. an SVG-export renderer
    /// that implements <see cref="IChartRenderer"/> directly).
    /// </summary>
    /// <remarks>
    /// This is a plain <see cref="Type"/> (framework-neutral). A Blazor renderer package sets it
    /// to its component type so a host can resolve the renderer from the enumerable
    /// <see cref="ChartRendererTypes"/> registry without a separate map — no reflection, no module
    /// initialiser. The contract layer does not load or instantiate the type.
    /// </remarks>
    Type? RenderComponentType { get; }
}
