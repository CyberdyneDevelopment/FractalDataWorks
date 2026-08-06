using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Rendering;

/// <summary>
/// UI renderer type for RazorConsole VDOM rendering.
/// </summary>
/// <remarks>
/// RazorConsole provides a virtual DOM-based terminal UI with .razor component syntax.
/// Supports focus management and hot reload for dynamic UIs.
/// </remarks>
[TypeOption(typeof(UIRenderers), "RazorConsole", RestrictToCurrentCompilation = true)]
public sealed class RazorConsoleUIRendererType : UIRendererBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RazorConsoleUIRendererType"/> class.
    /// </summary>
    public RazorConsoleUIRendererType()
        : base(
            id: 2,
            name: "RazorConsole",
            displayName: "RazorConsole VDOM",
            description: "Terminal UI rendering using RazorConsole virtual DOM with .razor component syntax")
    {
    }

    /// <inheritdoc />
    public override bool SupportsInteractiveMode => true;

    /// <inheritdoc />
    public override bool SupportsAnsiColors => true;

    /// <inheritdoc />
    public override bool SupportsFocusManagement => true;

    /// <inheritdoc />
    public override bool SupportsHotReload => true;
}
