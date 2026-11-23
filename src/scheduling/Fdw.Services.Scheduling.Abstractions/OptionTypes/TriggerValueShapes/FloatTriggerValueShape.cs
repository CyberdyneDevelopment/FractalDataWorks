using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>A float: a whole number when it has no fraction and fits an int.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TriggerValueShapes), "Float")]
public sealed class FloatTriggerValueShape : TriggerValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="FloatTriggerValueShape"/> class.</summary>
    public FloatTriggerValueShape()
        : base("Float")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is float;

    /// <inheritdoc />
    public override bool TryWindowInt(object value, out int result)
    {
        result = 0;
        return false;
    }

    /// <inheritdoc />
    public override bool TryInt(object value, out int result)
    {
        var single = (float)value;
        var fits = single == Math.Truncate(single) && single >= int.MinValue && single <= int.MaxValue;
        result = fits ? (int)single : 0;
        return fits;
    }
}
