using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// TypeCollection of the units an amount of time is counted in. A developer adds a unit by adding an option.
/// </summary>
[TypeCollection(typeof(TimeUnitBase), typeof(ITimeUnit), typeof(TimeUnits))]
[ExcludeFromCodeCoverage]
public abstract partial class TimeUnits : TypeCollectionBase<TimeUnitBase, ITimeUnit>
{
}
