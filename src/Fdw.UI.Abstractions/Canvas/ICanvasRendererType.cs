using System;
using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// Interface for canvas renderer type options.
/// </summary>
/// <remarks>
/// <para>
/// Each registered renderer type carries its capability flags and supported layout algorithms.
/// The type option is the descriptor; the actual renderer instance is obtained from DI or a
/// factory — it is not part of the TypeCollection entry.
/// </para>
/// <para>
/// The <see cref="CanvasRendererTypes"/> TypeCollection enables runtime enumeration for a
/// renderer-selection dropdown: <c>CanvasRendererTypes.All()</c> lists every registered renderer.
/// </para>
/// <para>
/// Compare against <see cref="CanvasRendererTypes.NotFound"/> — never null.
/// </para>
/// </remarks>
public interface ICanvasRendererType : ITypeOption<int, CanvasRendererTypeBase>
{
    /// <summary>
    /// Gets the human-readable display name shown in the renderer-selection dropdown.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets a value indicating whether this renderer supports edit-mode interactions.
    /// </summary>
    bool SupportsEditing { get; }

    /// <summary>
    /// Gets a value indicating whether this renderer renders individual ports on nodes.
    /// </summary>
    bool SupportsPorts { get; }

    /// <summary>
    /// Gets a value indicating whether this renderer is optimised for large graphs
    /// (hundreds of nodes without significant performance degradation).
    /// </summary>
    bool SupportsLargeGraphs { get; }

    /// <summary>
    /// Gets the layout algorithm names this renderer supports (e.g. "dagre", "force", "manual").
    /// </summary>
    IReadOnlyList<string> LayoutAlgorithms { get; }

    /// <summary>
    /// Gets the CLR type of the UI component that renders this canvas, or <see langword="null"/>
    /// for renderers that have no component-tree representation (e.g. a TUI/string renderer that
    /// implements <see cref="ICanvasRenderer"/> directly).
    /// </summary>
    /// <remarks>
    /// This is a plain <see cref="Type"/> (framework-neutral). A Blazor renderer package sets it to
    /// its component type so a host can resolve the renderer from the enumerable
    /// <see cref="CanvasRendererTypes"/> registry without a separate map — no reflection, no module
    /// initializer. The contract layer does not load or instantiate the type.
    /// </remarks>
    Type? RenderComponentType { get; }
}
