using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// Base class for canvas renderer type options using the CRTP pattern.
/// </summary>
/// <remarks>
/// Inherit from this class and apply <c>[TypeOption(typeof(CanvasRendererTypes), "YourName")]</c>
/// to register a new renderer with the enumerable registry. Capability flags and layout algorithm
/// names are set via constructor arguments — no property overrides.
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class CanvasRendererTypeBase : TypeOptionBase<int, CanvasRendererTypeBase>, ICanvasRendererType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CanvasRendererTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique numeric identifier for this renderer type.</param>
    /// <param name="name">The registry name (used by <c>CanvasRendererTypes.ByName()</c>).</param>
    /// <param name="displayName">The human-readable name shown in the dropdown.</param>
    /// <param name="supportsEditing">Whether this renderer handles edit-mode interactions.</param>
    /// <param name="supportsPorts">Whether this renderer renders port connectors on nodes.</param>
    /// <param name="supportsLargeGraphs">Whether this renderer stays performant on large graphs.</param>
    /// <param name="layoutAlgorithms">Layout algorithm names supported by this renderer.</param>
    /// <param name="renderComponentType">
    /// The CLR type of the UI component that renders this canvas, or <see langword="null"/> for
    /// renderers with no component-tree representation (e.g. a TUI renderer implementing
    /// <see cref="ICanvasRenderer"/> directly). A Blazor renderer passes its component type here.
    /// </param>
    protected CanvasRendererTypeBase(
        int id,
        string name,
        string displayName,
        bool supportsEditing,
        bool supportsPorts,
        bool supportsLargeGraphs,
        IReadOnlyList<string> layoutAlgorithms,
        Type? renderComponentType = null)
        : base(id, name, name, displayName, displayName, category: null)
    {
        SupportsEditing = supportsEditing;
        SupportsPorts = supportsPorts;
        SupportsLargeGraphs = supportsLargeGraphs;
        LayoutAlgorithms = layoutAlgorithms;
        RenderComponentType = renderComponentType;
    }

    /// <inheritdoc />
    public bool SupportsEditing { get; }

    /// <inheritdoc />
    public bool SupportsPorts { get; }

    /// <inheritdoc />
    public bool SupportsLargeGraphs { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> LayoutAlgorithms { get; }

    /// <inheritdoc />
    public Type? RenderComponentType { get; }
}
