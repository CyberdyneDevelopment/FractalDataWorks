using System;
using Fdw.Collections.Attributes;
using Fdw.UI.Abstractions.Components;
using Fdw.UI.Components.Models;
using Fdw.UI.Components.TypeIntegration;
using Fdw.UI.Rendering.Blazor.Components;

namespace Fdw.UI.Rendering.Blazor.Dispatch;

/// <summary>Renders <see cref="TextInputModel"/>.</summary>
[TypeOption(typeof(BlazorComponentRenderers), "TextInput")]
public sealed class TextInputComponentRenderer : BlazorComponentRendererBase
{
    /// <summary>Initializes a new instance of the <see cref="TextInputComponentRenderer"/> class.</summary>
    public TextInputComponentRenderer() : base(id: 1, name: "TextInput", precedence: 10) { }

    /// <inheritdoc />
    public override Type ComponentType => typeof(FdwTextInput);

    /// <inheritdoc />
    public override bool CanRender(IComponentModel model) => model is TextInputModel;
}
