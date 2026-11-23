using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>A double: a whole number when it has no fraction and fits an int.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TriggerValueShapes), "Double")]
public sealed class DoubleTriggerValueShape : TriggerValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="DoubleTriggerValueShape"/> class.</summary>
    public DoubleTriggerValueShape()
        : base("Double")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is double;

    /// <inheritdoc />
    public override bool TryInt(object value, out int result)
    {
        var floating = (double)value;
        var fits = floating == Math.Truncate(floating) && floating >= int.MinValue && floating <= int.MaxValue;
        result = fits ? (int)floating : 0;
        return fits;
    }
}
