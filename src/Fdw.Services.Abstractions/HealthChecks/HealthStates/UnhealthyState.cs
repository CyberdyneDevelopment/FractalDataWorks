using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Abstractions.Health;

/// <summary>
/// Unhealthy state - service is not functioning properly.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HealthStates), "Unhealthy", RestrictToCurrentCompilation = true)]
public sealed class UnhealthyState : HealthStateBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnhealthyState"/> class.
    /// </summary>
    public UnhealthyState() : base(2, "Unhealthy", isHealthy: false)
    {
    }
}
