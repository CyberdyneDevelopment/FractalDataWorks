using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>An int: a whole number, and 0 or 1 as a true or false.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TriggerValueShapes), "Int")]
public sealed class IntTriggerValueShape : TriggerValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="IntTriggerValueShape"/> class.</summary>
    public IntTriggerValueShape()
        : base("Int")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is int;

    /// <inheritdoc />
    public override bool TryInt(object value, out int result)
    {
        result = (int)value;
        return true;
    }

    /// <inheritdoc />
    public override bool TryNarrowInt(object value, out int result) => TryInt(value, out result);

    /// <inheritdoc />
    public override bool TryBool(object value, out bool result)
    {
        result = (int)value == 1;
        return (int)value == 0 || (int)value == 1;
    }
}
