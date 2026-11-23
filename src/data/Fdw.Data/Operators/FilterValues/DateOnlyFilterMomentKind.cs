using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>A calendar date, as midnight.</summary>
[TypeOption(typeof(FilterMomentKinds), "Date")]
[ExcludeFromCodeCoverage]
public sealed class DateOnlyFilterMomentKind : FilterMomentKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DateOnlyFilterMomentKind"/> class.</summary>
    public DateOnlyFilterMomentKind()
        : base("Date")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateOnly;

    /// <inheritdoc />
    public override DateTimeOffset ToMoment(object value) => new(((DateOnly)value).ToDateTime(TimeOnly.MinValue));
}
