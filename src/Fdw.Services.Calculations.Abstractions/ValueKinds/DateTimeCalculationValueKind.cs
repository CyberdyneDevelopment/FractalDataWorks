using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// A moment in time. The results array carries it as its UTC ticks, which a decimal holds exactly; reading it gives the moment as a
/// <see cref="DateTimeOffset"/> at offset zero.
/// </summary>
[TypeOption(typeof(CalculationValueKinds), "DateTime")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeCalculationValueKind : CalculationValueKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeCalculationValueKind"/> class.
    /// </summary>
    public DateTimeCalculationValueKind()
        : base("DateTime", typeof(DateTimeOffset), "DateTimeOffset")
    {
    }

    /// <inheritdoc />
    public override object Read(decimal carried) => new DateTimeOffset(decimal.ToInt64(carried), TimeSpan.Zero);
}
