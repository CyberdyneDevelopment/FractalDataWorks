using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.Health;

/// <summary>A window stated in days (<c>d</c>).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HealthWindowUnits), "Days")]
public sealed class DaysHealthWindowUnit : HealthWindowUnitBase
{
    /// <summary>Initializes a new instance of the <see cref="DaysHealthWindowUnit"/> class.</summary>
    public DaysHealthWindowUnit()
        : base("Days", 'd', TimeSpan.FromDays(1))
    {
    }
}
