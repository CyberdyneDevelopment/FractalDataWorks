using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Etl.Abstractions.Monitoring.HealthStateOptions;

/// <summary>
/// Base class for health states.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class HealthStateBase : TypeOptionBase<int, HealthStateBase>, IHealthState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HealthStateBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this health state.</param>
    /// <param name="name">The name of this health state.</param>
    /// <param name="isHealthy">Whether this state represents a healthy condition.</param>
    /// <param name="requiresAttention">Whether this state requires attention.</param>
    protected HealthStateBase(int id, string name, bool isHealthy, bool requiresAttention)
        : base(id, name)
    {
        IsHealthy = isHealthy;
        RequiresAttention = requiresAttention;
    }

    /// <inheritdoc />
    public bool IsHealthy { get; }

    /// <inheritdoc />
    public bool RequiresAttention { get; }
}
