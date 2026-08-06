using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.HealthStateOptions;

/// <summary>
/// Service is degraded but functional.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HealthStates), "Degraded", RestrictToCurrentCompilation = true)]
public sealed class DegradedState : HealthStateBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DegradedState"/> class.
    /// </summary>
    public DegradedState() : base(1, "Degraded", isHealthy: false, requiresAttention: true) { }
}
