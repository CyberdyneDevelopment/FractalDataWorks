using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.Health;

/// <summary>A window stated in minutes (<c>m</c>).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HealthWindowUnits), "Minutes")]
public sealed class MinutesHealthWindowUnit : HealthWindowUnitBase
{
    /// <summary>Initializes a new instance of the <see cref="MinutesHealthWindowUnit"/> class.</summary>
    public MinutesHealthWindowUnit()
        : base("Minutes", 'm', TimeSpan.FromMinutes(1))
    {
    }
}
