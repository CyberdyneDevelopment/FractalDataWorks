using System;
using Fdw.Collections.Attributes;
using Fdw.UI.Abstractions.Components;
using Fdw.UI.Components.Models;
using Fdw.UI.Components.TypeIntegration;
using Fdw.UI.Rendering.Blazor.Components;

namespace Fdw.UI.Rendering.Blazor.Dispatch;

/// <summary>Renders any <see cref="IMultiSelectComponentModel"/>.</summary>
[TypeOption(typeof(BlazorComponentRenderers), "MultiSelect")]
public sealed class MultiSelectComponentRenderer : BlazorComponentRendererBase
{
    /// <summary>Initializes a new instance of the <see cref="MultiSelectComponentRenderer"/> class.</summary>
    public MultiSelectComponentRenderer() : base(id: 7, name: "MultiSelect", precedence: 40) { }

    /// <inheritdoc />
    public override Type ComponentType => typeof(FdwMultiSelect);

    /// <inheritdoc />
    public override bool CanRender(IComponentModel model) => model is IMultiSelectComponentModel;
}
