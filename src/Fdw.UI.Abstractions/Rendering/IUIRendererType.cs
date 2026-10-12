using Fdw.Collections;

namespace Fdw.UI.Abstractions.Rendering;

/// <summary>
/// Interface for UI renderer type options.
/// </summary>
public interface IUIRendererType : ITypeOption<int, UIRendererBase>
{
    /// <summary>
    /// Gets the display name for this renderer.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets a value indicating whether this renderer supports interactive mode.
    /// </summary>
    bool SupportsInteractiveMode { get; }

    /// <summary>
    /// Gets a value indicating whether this renderer supports ANSI colors.
    /// </summary>
    bool SupportsAnsiColors { get; }

    /// <summary>
    /// Gets a value indicating whether this renderer supports focus management.
    /// </summary>
    bool SupportsFocusManagement { get; }

    /// <summary>
    /// Gets a value indicating whether this renderer supports hot reload.
    /// </summary>
    bool SupportsHotReload { get; }

    /// <summary>
    /// Gets a description of this renderer.
    /// </summary>
    string Description { get; }
}