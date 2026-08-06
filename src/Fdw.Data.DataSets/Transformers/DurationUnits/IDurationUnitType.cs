using System;
using Fdw.Collections;

namespace Fdw.Data.DataSets;

/// <summary>
/// Interface for duration unit type options used by field transforms.
/// </summary>
public interface IDurationUnitType : ITypeOption<int, IDurationUnitType>
{
    /// <summary>
    /// Creates a <see cref="TimeSpan"/> for the given amount in this unit.
    /// </summary>
    /// <param name="amount">The numeric amount.</param>
    /// <returns>The corresponding TimeSpan.</returns>
    TimeSpan ToTimeSpan(double amount);
}
