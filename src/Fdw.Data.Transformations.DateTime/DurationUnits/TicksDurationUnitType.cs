using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>
/// Ticks duration unit (FDW-1983): a .NET tick, 100 nanoseconds, the smallest step a .NET or SQL Server datetime2 time holds. It takes whole
/// ticks only; an amount that is not a whole number, or does not fit a 64-bit integer, is refused and never rounded.
/// </summary>
[TypeOption(typeof(DurationUnitTypes), "Ticks")]
public sealed class TicksDurationUnitType : DurationUnitTypeBase
{
    public TicksDurationUnitType() : base("Ticks") { }

    /// <inheritdoc/>
    public override TimeSpan ToTimeSpan(double amount)
    {
        if (amount != Math.Floor(amount) || amount < long.MinValue || amount > long.MaxValue)
        {
            throw new InvalidOperationException($"A duration in Ticks takes a whole number of ticks that fits a 64-bit integer; received {amount.ToString(System.Globalization.CultureInfo.InvariantCulture)}.");
        }

        return TimeSpan.FromTicks((long)amount);
    }
}
