using System;
using Fdw.Collections.Attributes;
using Fdw.UI.Abstractions.Components;
using Fdw.UI.Components.Models;
using Fdw.UI.Components.TypeIntegration;
using Fdw.UI.Rendering.Blazor.Components;

namespace Fdw.UI.Rendering.Blazor.Dispatch;

/// <summary>Renders any <see cref="ISelectableComponentModel"/> not claimed by a concrete mapping.</summary>
[TypeOption(typeof(BlazorComponentRenderers), "Select")]
public sealed class SelectComponentRenderer : BlazorComponentRendererBase
{
    /// <summary>Initializes a new instance of the <see cref="SelectComponentRenderer"/> class.</summary>
    public SelectComponentRenderer() : base(id: 6, name: "Select", precedence: 30) { }

    /// <inheritdoc />
    public override Type ComponentType => typeof(FdwSelect);

    /// <inheritdoc />
    public override bool CanRender(IComponentModel model) => model is ISelectableComponentModel;
}
