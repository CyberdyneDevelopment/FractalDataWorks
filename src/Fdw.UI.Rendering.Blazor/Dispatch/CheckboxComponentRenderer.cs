using System;
using Fdw.Collections.Attributes;
using Fdw.UI.Abstractions.Components;
using Fdw.UI.Components.Models;
using Fdw.UI.Components.TypeIntegration;
using Fdw.UI.Rendering.Blazor.Components;

namespace Fdw.UI.Rendering.Blazor.Dispatch;

/// <summary>Renders <see cref="CheckboxModel"/>.</summary>
[TypeOption(typeof(BlazorComponentRenderers), "Checkbox")]
public sealed class CheckboxComponentRenderer : BlazorComponentRendererBase
{
    /// <summary>Initializes a new instance of the <see cref="CheckboxComponentRenderer"/> class.</summary>
    public CheckboxComponentRenderer() : base(id: 2, name: "Checkbox", precedence: 10) { }

    /// <inheritdoc />
    public override Type ComponentType => typeof(FdwCheckbox);

    /// <inheritdoc />
    public override bool CanRender(IComponentModel model) => model is CheckboxModel;
}
