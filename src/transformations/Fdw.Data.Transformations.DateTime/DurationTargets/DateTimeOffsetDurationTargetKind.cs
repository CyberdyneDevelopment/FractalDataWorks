using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>A date and time with an offset.</summary>
[TypeOption(typeof(DurationTargetKinds), "DateTimeOffset")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeOffsetDurationTargetKind : DurationTargetKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeOffsetDurationTargetKind"/> class.</summary>
    public DateTimeOffsetDurationTargetKind()
        : base("DateTimeOffset")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateTimeOffset;

    /// <inheritdoc />
    public override object Add(object value, TimeSpan duration) => ((DateTimeOffset)value).Add(duration);
}
