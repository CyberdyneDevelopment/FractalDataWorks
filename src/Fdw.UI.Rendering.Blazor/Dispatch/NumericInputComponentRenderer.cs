using System;
using Fdw.Collections.Attributes;
using Fdw.UI.Abstractions.Components;
using Fdw.UI.Components.Models;
using Fdw.UI.Components.TypeIntegration;
using Fdw.UI.Rendering.Blazor.Components;

namespace Fdw.UI.Rendering.Blazor.Dispatch;

/// <summary>
/// Renders any numeric <see cref="IInputComponentModel"/>.
/// </summary>
/// <remarks>
/// Why precedence 20 rather than 10: this matches by VALUE TYPE across every input model, so it
/// would also claim a TextInputModel whose value happened to be numeric. It must be consulted only
/// after the concrete mappings have had their chance — the same order the previous switch encoded
/// positionally.
/// </remarks>
[TypeOption(typeof(BlazorComponentRenderers), "NumericInput")]
public sealed class NumericInputComponentRenderer : BlazorComponentRendererBase
{
    /// <summary>Initializes a new instance of the <see cref="NumericInputComponentRenderer"/> class.</summary>
    public NumericInputComponentRenderer() : base(id: 5, name: "NumericInput", precedence: 20) { }

    /// <inheritdoc />
    public override Type ComponentType => typeof(FdwNumericInput);

    /// <inheritdoc />
    public override bool CanRender(IComponentModel model) =>
        model is IInputComponentModel input && IsNumeric(input);

    private static bool IsNumeric(IInputComponentModel model)
    {
        var valueType = Nullable.GetUnderlyingType(model.ValueType) ?? model.ValueType;
        return valueType == typeof(int) || valueType == typeof(long) ||
               valueType == typeof(decimal) || valueType == typeof(double) ||
               valueType == typeof(float) || valueType == typeof(short) ||
               valueType == typeof(byte);
    }
}
