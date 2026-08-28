using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// Base class for canvas edge types using the CRTP pattern.
/// </summary>
/// <remarks>
/// Inherit from this class and apply <c>[TypeOption(typeof(CanvasEdgeTypes), "YourName")]</c>
/// to register a new canvas edge kind.
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class CanvasEdgeTypeBase : TypeOptionBase<int, CanvasEdgeTypeBase>, ICanvasEdgeType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CanvasEdgeTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique numeric identifier for this edge type.</param>
    /// <param name="name">The registry name (used by <c>CanvasEdgeTypes.ByName()</c>).</param>
    /// <param name="displayName">The human-readable display name shown in the renderer.</param>
    /// <param name="iconHint">The icon or line-style hint string passed to the renderer.</param>
    protected CanvasEdgeTypeBase(int id, string name, string displayName, string iconHint)
        : base(id, name, name, displayName, displayName, category: null)
    {
        IconHint = iconHint;
    }

    /// <inheritdoc />
    public string IconHint { get; }
}
