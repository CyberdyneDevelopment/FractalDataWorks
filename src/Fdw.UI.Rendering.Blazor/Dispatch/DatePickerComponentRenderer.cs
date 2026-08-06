using System;
using Fdw.Collections.Attributes;
using Fdw.UI.Abstractions.Components;
using Fdw.UI.Components.Models;
using Fdw.UI.Components.TypeIntegration;
using Fdw.UI.Rendering.Blazor.Components;

namespace Fdw.UI.Rendering.Blazor.Dispatch;

/// <summary>Renders <see cref="DatePickerModel"/>.</summary>
[TypeOption(typeof(BlazorComponentRenderers), "DatePicker")]
public sealed class DatePickerComponentRenderer : BlazorComponentRendererBase
{
    /// <summary>Initializes a new instance of the <see cref="DatePickerComponentRenderer"/> class.</summary>
    public DatePickerComponentRenderer() : base(id: 3, name: "DatePicker", precedence: 10) { }

    /// <inheritdoc />
    public override Type ComponentType => typeof(FdwDatePicker);

    /// <inheritdoc />
    public override bool CanRender(IComponentModel model) => model is DatePickerModel;
}
