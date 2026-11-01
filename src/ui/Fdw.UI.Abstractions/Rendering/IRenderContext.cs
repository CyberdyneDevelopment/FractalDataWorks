using Fdw.UI.Abstractions.RenderModeOptions;

namespace Fdw.UI.Abstractions.Rendering;

/// <summary>
/// Context for rendering operations.
/// </summary>
public interface IRenderContext
{
    /// <summary>
    /// Gets the current render mode (Display, Edit, ReadOnly).
    /// </summary>
    IRenderMode Mode { get; }

    /// <summary>
    /// Gets the current theme.
    /// </summary>
    object? Theme { get; }

    /// <summary>
    /// Gets the console width, if available.
    /// </summary>
    int? ConsoleWidth { get; }

    /// <summary>
    /// Gets the console height, if available.
    /// </summary>
    int? ConsoleHeight { get; }

    /// <summary>
    /// Gets a value indicating whether the output supports Unicode.
    /// </summary>
    bool SupportsUnicode { get; }

    /// <summary>
    /// Gets additional context data.
    /// </summary>
    System.Collections.Generic.IDictionary<string, object> Data { get; }
}