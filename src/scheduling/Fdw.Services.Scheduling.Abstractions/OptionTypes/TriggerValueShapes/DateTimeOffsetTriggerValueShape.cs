using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>A DateTimeOffset: its local date and time, without the offset.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TriggerValueShapes), "DateTimeOffset")]
public sealed class DateTimeOffsetTriggerValueShape : TriggerValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeOffsetTriggerValueShape"/> class.</summary>
    public DateTimeOffsetTriggerValueShape()
        : base("DateTimeOffset")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is DateTimeOffset;

    /// <inheritdoc />
    public override bool TryDateTime(object value, out DateTime result)
    {
        result = ((DateTimeOffset)value).DateTime;
        return true;
    }
}
