using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A DateTime.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordValueShapes), "Time")]
public sealed class TimeRecordValueShape : RecordValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="TimeRecordValueShape"/> class.</summary>
    public TimeRecordValueShape()
        : base("Time")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is DateTime;

    /// <inheritdoc />
    /// <remarks>A DateTime that states no kind is read as UTC, the same rule the DateTimeOffset parameter type applies to a text without an offset.</remarks>
    public override bool TryMoment(object value, out DateTimeOffset moment)
    {
        var time = (DateTime)value;
        moment = new DateTimeOffset(time.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(time, DateTimeKind.Utc) : time);
        return true;
    }
}
