using Fdw.Collections;

namespace Fdw.UI.Abstractions.RenderModeOptions;

/// <summary>
/// Interface for component rendering modes.
/// Extends ITypeOption to enable TypeCollection discovery.
/// </summary>
public interface IRenderMode : ITypeOption<int, RenderModeBase>
{
    /// <summary>
    /// Gets a value indicating whether this mode allows editing.
    /// </summary>
    bool AllowsEditing { get; }

    /// <summary>
    /// Gets a value indicating whether this mode shows view.
    /// </summary>
    bool ShowsView { get; }
}
