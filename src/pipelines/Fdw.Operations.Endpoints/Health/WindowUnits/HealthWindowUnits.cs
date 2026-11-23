using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.Health;

/// <summary>
/// The units a health query's time window is stated in: minutes (<c>m</c>), hours (<c>h</c>) and days (<c>d</c>).
/// </summary>
/// <remarks>A window text ends in the unit's symbol, matched exactly through <c>BySymbol</c>; a symbol that matches none gets <c>NotFound</c> and the window is refused.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(HealthWindowUnitBase), typeof(IHealthWindowUnit), typeof(HealthWindowUnits))]
public abstract partial class HealthWindowUnits : TypeCollectionBase<HealthWindowUnitBase, IHealthWindowUnit>
{
}
