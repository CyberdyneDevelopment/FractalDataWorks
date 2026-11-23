using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>A decimal: a whole number when it has no fraction and fits an int.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TriggerValueShapes), "Decimal")]
public sealed class DecimalTriggerValueShape : TriggerValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="DecimalTriggerValueShape"/> class.</summary>
    public DecimalTriggerValueShape()
        : base("Decimal")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is decimal;

    /// <inheritdoc />
    public override bool TryInt(object value, out int result)
    {
        var exact = (decimal)value;
        var fits = exact == Math.Truncate(exact) && exact >= int.MinValue && exact <= int.MaxValue;
        result = fits ? (int)exact : 0;
        return fits;
    }
}
