using System;
using Fdw.Collections.Attributes;
using Fdw.UI.Abstractions.Components;
using Fdw.UI.Components.Models;
using Fdw.UI.Components.TypeIntegration;
using Fdw.UI.Rendering.Blazor.Components;

namespace Fdw.UI.Rendering.Blazor.Dispatch;

/// <summary>Renders <see cref="TypeCollectionSelectModel"/>.</summary>
[TypeOption(typeof(BlazorComponentRenderers), "TypeCollectionSelect")]
public sealed class TypeCollectionSelectComponentRenderer : BlazorComponentRendererBase
{
    /// <summary>Initializes a new instance of the <see cref="TypeCollectionSelectComponentRenderer"/> class.</summary>
    public TypeCollectionSelectComponentRenderer() : base(id: 4, name: "TypeCollectionSelect", precedence: 10) { }

    /// <inheritdoc />
    public override Type ComponentType => typeof(FdwSelect);

    /// <inheritdoc />
    public override bool CanRender(IComponentModel model) => model is TypeCollectionSelectModel;
}
