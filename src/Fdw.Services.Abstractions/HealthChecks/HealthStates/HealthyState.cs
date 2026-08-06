using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Abstractions.Health;

/// <summary>
/// Healthy state - service is functioning normally.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HealthStates), "Healthy", RestrictToCurrentCompilation = true)]
public sealed class HealthyState : HealthStateBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HealthyState"/> class.
    /// </summary>
    public HealthyState() : base(1, "Healthy", isHealthy: true)
    {
    }
}
