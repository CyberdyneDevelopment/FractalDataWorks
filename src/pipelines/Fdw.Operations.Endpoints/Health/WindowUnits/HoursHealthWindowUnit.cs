using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.Health;

/// <summary>A window stated in hours (<c>h</c>).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HealthWindowUnits), "Hours")]
public sealed class HoursHealthWindowUnit : HealthWindowUnitBase
{
    /// <summary>Initializes a new instance of the <see cref="HoursHealthWindowUnit"/> class.</summary>
    public HoursHealthWindowUnit()
        : base("Hours", 'h', TimeSpan.FromHours(1))
    {
    }
}
