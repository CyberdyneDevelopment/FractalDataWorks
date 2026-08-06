using Fdw.Collections;

namespace Fdw.Services.Abstractions.Health;

/// <summary>
/// Base class for health state TypeOptions.
/// </summary>
public abstract class HealthStateBase : TypeOptionBase<int, HealthStateBase>, IHealthState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HealthStateBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The health state name.</param>
    /// <param name="isHealthy">Whether this represents a healthy state.</param>
    protected HealthStateBase(int id, string name, bool isHealthy)
        : base(id, name, $"HealthStates:{name}", name, $"{name} health state", "HealthChecks")
    {
        IsHealthy = isHealthy;
    }

    /// <inheritdoc/>
    public bool IsHealthy { get; }
}
