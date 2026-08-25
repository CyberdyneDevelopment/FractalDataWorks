using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Days duration unit.
/// </summary>
[TypeOption(typeof(DurationUnitTypes), "Days")]
public sealed class DaysDurationUnitType : DurationUnitTypeBase
{
    public DaysDurationUnitType() : base(3, "Days") { }

    /// <inheritdoc/>
    public override TimeSpan ToTimeSpan(double amount) => TimeSpan.FromDays(amount);
}
