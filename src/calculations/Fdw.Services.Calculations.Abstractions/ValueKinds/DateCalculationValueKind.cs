using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// A calendar date with no time of day. The results array carries it as its day number, which a decimal holds exactly; reading it
/// gives the date as a <see cref="DateOnly"/>.
/// </summary>
[TypeOption(typeof(CalculationValueKinds), "Date")]
[ExcludeFromCodeCoverage]
public sealed class DateCalculationValueKind : CalculationValueKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DateCalculationValueKind"/> class.
    /// </summary>
    public DateCalculationValueKind()
        : base("Date", typeof(DateOnly), "Date")
    {
    }

    /// <inheritdoc />
    public override object Read(decimal carried) => DateOnly.FromDayNumber(decimal.ToInt32(carried));
}
