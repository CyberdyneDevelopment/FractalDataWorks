using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>A date and time.</summary>
[TypeOption(typeof(FilterMomentKinds), "DateTime")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeFilterMomentKind : FilterMomentKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeFilterMomentKind"/> class.</summary>
    public DateTimeFilterMomentKind()
        : base("DateTime")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateTime;

    /// <inheritdoc />
    public override DateTimeOffset ToMoment(object value) => new((DateTime)value);
}
