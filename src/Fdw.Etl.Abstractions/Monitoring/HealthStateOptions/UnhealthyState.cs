using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.HealthStateOptions;

/// <summary>
/// Service is unhealthy.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HealthStates), "Unhealthy", RestrictToCurrentCompilation = true)]
public sealed class UnhealthyState : HealthStateBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnhealthyState"/> class.
    /// </summary>
    public UnhealthyState() : base(2, "Unhealthy", isHealthy: false, requiresAttention: true) { }
}
