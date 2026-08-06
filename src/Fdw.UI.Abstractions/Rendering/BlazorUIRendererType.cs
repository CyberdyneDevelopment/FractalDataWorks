using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Rendering;

/// <summary>
/// UI renderer type for Blazor rendering.
/// </summary>
/// <remarks>
/// Blazor renders component models as retained-mode RenderFragments hosted by a Blazor
/// circuit (Server) or WebAssembly runtime. Interactive input completes asynchronously
/// via bound components rather than blocking prompts.
/// </remarks>
[TypeOption(typeof(UIRenderers), "Blazor", RestrictToCurrentCompilation = true)]
public sealed class BlazorUIRendererType : UIRendererBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorUIRendererType"/> class.
    /// </summary>
    public BlazorUIRendererType()
        : base(
            id: 3,
            name: "Blazor",
            displayName: "Blazor",
            description: "Retained-mode web UI rendering using Blazor RenderFragments with bound input components")
    {
    }

    /// <inheritdoc />
    public override bool SupportsInteractiveMode => true;

    /// <inheritdoc />
    public override bool SupportsAnsiColors => false;

    /// <inheritdoc />
    public override bool SupportsFocusManagement => true;

    /// <inheritdoc />
    public override bool SupportsHotReload => true;
}
