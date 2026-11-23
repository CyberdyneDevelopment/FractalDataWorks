using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>A date and time.</summary>
[TypeOption(typeof(DurationTargetKinds), "DateTime")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeDurationTargetKind : DurationTargetKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeDurationTargetKind"/> class.</summary>
    public DateTimeDurationTargetKind()
        : base("DateTime")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateTime;

    /// <inheritdoc />
    public override object Add(object value, TimeSpan duration) => ((DateTime)value).Add(duration);
}
