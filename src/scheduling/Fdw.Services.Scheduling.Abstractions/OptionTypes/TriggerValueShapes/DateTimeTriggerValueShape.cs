using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>A DateTime, as it is.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TriggerValueShapes), "DateTime")]
public sealed class DateTimeTriggerValueShape : TriggerValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeTriggerValueShape"/> class.</summary>
    public DateTimeTriggerValueShape()
        : base("DateTime")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is DateTime;

    /// <inheritdoc />
    public override bool TryDateTime(object value, out DateTime result)
    {
        result = (DateTime)value;
        return true;
    }
}
