using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Seconds duration unit.
/// </summary>
[TypeOption(typeof(DurationUnitTypes), "Seconds")]
public sealed class SecondsDurationUnitType : DurationUnitTypeBase
{
    public SecondsDurationUnitType() : base(4, "Seconds") { }

    /// <inheritdoc/>
    public override TimeSpan ToTimeSpan(double amount) => TimeSpan.FromSeconds(amount);
}
