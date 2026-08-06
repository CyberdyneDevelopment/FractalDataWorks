using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// Base class for chart renderer type options using the CRTP pattern.
/// </summary>
/// <remarks>
/// Inherit from this class and apply <c>[TypeOption(typeof(ChartRendererTypes), "YourName")]</c>
/// to register a new renderer with the enumerable registry. Capability flags, the supported chart
/// type list, and the render component type are set via constructor arguments — no property overrides.
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class ChartRendererTypeBase : TypeOptionBase<int, ChartRendererTypeBase>, IChartRendererType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChartRendererTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique numeric identifier for this renderer type.</param>
    /// <param name="name">The registry name (used by <c>ChartRendererTypes.ByName()</c>).</param>
    /// <param name="displayName">The human-readable name shown in the dropdown.</param>
    /// <param name="supportsInteraction">Whether this renderer supports interactive user interactions.</param>
    /// <param name="supportsLargeSeries">Whether this renderer stays performant on large series.</param>
    /// <param name="supportsEditing">Whether this renderer supports edit-mode configuration controls.</param>
    /// <param name="supportedChartTypes">
    /// <see cref="ChartTypes"/> registry names of chart types this renderer can draw.
    /// Pass an empty list to indicate support for all registered chart types.
    /// </param>
    /// <param name="renderComponentType">
    /// The CLR type of the UI component that renders this chart, or <see langword="null"/> for
    /// renderers with no component-tree representation (e.g. an SVG-export renderer implementing
    /// <see cref="IChartRenderer"/> directly). A Blazor renderer passes its component type here.
    /// </param>
    // Why: DisplayName comes from TypeOptionBase via its 6-arg ctor; the capability flags and
    // SupportedChartTypes are net-new. Mirrors CanvasRendererTypeBase exactly.
    protected ChartRendererTypeBase(
        int id,
        string name,
        string displayName,
        bool supportsInteraction,
        bool supportsLargeSeries,
        bool supportsEditing,
        IReadOnlyList<string>? supportedChartTypes = null,
        Type? renderComponentType = null)
        : base(id, name, name, displayName, displayName, category: null)
    {
        SupportsInteraction = supportsInteraction;
        SupportsLargeSeries = supportsLargeSeries;
        SupportsEditing = supportsEditing;
        // Why: nullable-defaulted + `?? []` so the generated TypeCollection sentinel constructs without
        // a collection argument (TC009); sanctioned empty-collection fallback.
        SupportedChartTypes = supportedChartTypes ?? [];
        RenderComponentType = renderComponentType;
    }

    /// <inheritdoc />
    public bool SupportsInteraction { get; }

    /// <inheritdoc />
    public bool SupportsLargeSeries { get; }

    /// <inheritdoc />
    public bool SupportsEditing { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedChartTypes { get; }

    /// <inheritdoc />
    public Type? RenderComponentType { get; }
}
