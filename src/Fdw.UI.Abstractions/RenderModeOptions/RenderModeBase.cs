using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.RenderModeOptions;

/// <summary>
/// Base class for component rendering modes.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class RenderModeBase : TypeOptionBase<int, RenderModeBase>, IRenderMode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RenderModeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this render mode.</param>
    /// <param name="name">The name of this render mode.</param>
    /// <param name="allowsEditing">Whether this mode allows editing.</param>
    /// <param name="showsView">Whether this mode shows view.</param>
    protected RenderModeBase(int id, string name, bool allowsEditing, bool showsView)
        : base(id, name)
    {
        AllowsEditing = allowsEditing;
        ShowsView = showsView;
    }

    /// <inheritdoc />
    public bool AllowsEditing { get; }

    /// <inheritdoc />
    public bool ShowsView { get; }
}
