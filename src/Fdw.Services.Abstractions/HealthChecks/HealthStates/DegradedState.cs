using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Abstractions.Health;

/// <summary>
/// Degraded state - service is functioning but with reduced capability or performance.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HealthStates), "Degraded", RestrictToCurrentCompilation = true)]
public sealed class DegradedState : HealthStateBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DegradedState"/> class.
    /// </summary>
    public DegradedState() : base(3, "Degraded", isHealthy: false)
    {
    }
}
