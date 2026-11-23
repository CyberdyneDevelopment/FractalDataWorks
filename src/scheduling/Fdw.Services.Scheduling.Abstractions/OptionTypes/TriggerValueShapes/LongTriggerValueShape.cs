using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>A long: a whole number when it fits an int.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TriggerValueShapes), "Long")]
public sealed class LongTriggerValueShape : TriggerValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="LongTriggerValueShape"/> class.</summary>
    public LongTriggerValueShape()
        : base("Long")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is long;

    /// <inheritdoc />
    public override bool TryInt(object value, out int result)
    {
        var wide = (long)value;
        result = wide >= int.MinValue && wide <= int.MaxValue ? (int)wide : 0;
        return wide >= int.MinValue && wide <= int.MaxValue;
    }
}
