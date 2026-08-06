using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>
/// Minutes duration unit.
/// </summary>
[TypeOption(typeof(DurationUnitTypes), "Minutes")]
public sealed class MinutesDurationUnitType : DurationUnitTypeBase
{
    public MinutesDurationUnitType() : base(2, "Minutes") { }

    /// <inheritdoc/>
    public override TimeSpan ToTimeSpan(double amount) => TimeSpan.FromMinutes(amount);
}
