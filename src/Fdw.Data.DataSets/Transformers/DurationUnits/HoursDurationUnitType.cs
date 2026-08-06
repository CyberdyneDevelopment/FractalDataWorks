using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Hours duration unit.
/// </summary>
[TypeOption(typeof(DurationUnitTypes), "Hours")]
public sealed class HoursDurationUnitType : DurationUnitTypeBase
{
    public HoursDurationUnitType() : base(1, "Hours") { }

    /// <inheritdoc/>
    public override TimeSpan ToTimeSpan(double amount) => TimeSpan.FromHours(amount);
}
