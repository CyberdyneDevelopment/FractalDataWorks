using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>A date and time with an offset.</summary>
[TypeOption(typeof(FilterMomentKinds), "DateTimeOffset")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeOffsetFilterMomentKind : FilterMomentKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeOffsetFilterMomentKind"/> class.</summary>
    public DateTimeOffsetFilterMomentKind()
        : base("DateTimeOffset")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateTimeOffset;

    /// <inheritdoc />
    public override DateTimeOffset ToMoment(object value) => (DateTimeOffset)value;
}
