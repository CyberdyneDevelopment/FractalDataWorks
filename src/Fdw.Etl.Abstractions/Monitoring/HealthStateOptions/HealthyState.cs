using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.HealthStateOptions;

/// <summary>
/// Service is healthy.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HealthStates), "Healthy", RestrictToCurrentCompilation = true)]
public sealed class HealthyState : HealthStateBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HealthyState"/> class.
    /// </summary>
    public HealthyState() : base(0, "Healthy", isHealthy: true, requiresAttention: false) { }
}
