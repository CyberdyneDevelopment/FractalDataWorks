using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// Base class for canvas node types using the CRTP pattern.
/// </summary>
/// <remarks>
/// Inherit from this class and apply <c>[TypeOption(typeof(CanvasNodeTypes), "YourName")]</c>
/// to register a new canvas node kind. DisplayName and Category come from the
/// <see cref="TypeOptionBase{TKey,TValue}"/> base; <see cref="IconHint"/> is set via constructor —
/// no property overrides.
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class CanvasNodeTypeBase : TypeOptionBase<int, CanvasNodeTypeBase>, ICanvasNodeType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CanvasNodeTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique numeric identifier for this node type.</param>
    /// <param name="name">The registry name (used by <c>CanvasNodeTypes.ByName()</c>).</param>
    /// <param name="displayName">The human-readable display name shown in the renderer.</param>
    /// <param name="category">The category/group for renderer colour-coding and legend grouping.</param>
    /// <param name="iconHint">The icon hint string passed to the renderer.</param>
    protected CanvasNodeTypeBase(int id, string name, string displayName, string category, string iconHint)
        : base(id, name, name, displayName, displayName, category)
    {
        IconHint = iconHint;
    }

    /// <inheritdoc />
    public string IconHint { get; }
}
