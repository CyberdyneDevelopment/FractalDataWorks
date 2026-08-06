using Fdw.Collections;

namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// Interface for canvas node type options.
/// </summary>
/// <remarks>
/// Implemented by each domain node kind. Carries display metadata so renderers need no
/// switch/if-else on type names. Compare against <see cref="CanvasNodeTypes.NotFound"/> — never null.
/// </remarks>
public interface ICanvasNodeType : ITypeOption<int, CanvasNodeTypeBase>
{
    /// <summary>
    /// Gets the human-readable display name for this node type (e.g. "Data Store", "Pipeline").
    /// </summary>
    /// <remarks>
    /// <see cref="ITypeOption.Category"/> (inherited) carries the group used by renderers to
    /// colour-code or group nodes in a legend; it is not redeclared here.
    /// </remarks>
    string DisplayName { get; }

    /// <summary>
    /// Gets the icon hint string passed to the renderer (e.g. a Lucide icon name, an SVG id).
    /// </summary>
    /// <remarks>
    /// The meaning of this string is a convention between the domain that seeds node types and
    /// the renderer implementation. The canvas contract layer does not interpret it.
    /// </remarks>
    string IconHint { get; }
}
