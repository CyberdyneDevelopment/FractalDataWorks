using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Rendering;

/// <summary>
/// UI renderer type for Spectre.Console rendering.
/// </summary>
/// <remarks>
/// Spectre.Console provides direct console rendering with prompts, tables, and panels.
/// Best for terminal-based configuration UIs with full interactivity.
/// </remarks>
[TypeOption(typeof(UIRenderers), "Spectre", RestrictToCurrentCompilation = true)]
public sealed class SpectreUIRendererType : UIRendererBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpectreUIRendererType"/> class.
    /// </summary>
    public SpectreUIRendererType()
        : base(
            id: 1,
            name: "Spectre",
            displayName: "Spectre.Console",
            description: "Terminal UI rendering using Spectre.Console with prompts, tables, and panels")
    {
    }

    /// <inheritdoc />
    public override bool SupportsInteractiveMode => true;

    /// <inheritdoc />
    public override bool SupportsAnsiColors => true;

    /// <inheritdoc />
    public override bool SupportsFocusManagement => false;

    /// <inheritdoc />
    public override bool SupportsHotReload => false;
}
