using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Milliseconds duration unit (FDW-1983).
/// </summary>
[TypeOption(typeof(DurationUnitTypes), "Milliseconds")]
public sealed class MillisecondsDurationUnitType : DurationUnitTypeBase
{
    public MillisecondsDurationUnitType() : base("Milliseconds") { }

    /// <inheritdoc/>
    public override TimeSpan ToTimeSpan(double amount) => TimeSpan.FromMilliseconds(amount);
}
