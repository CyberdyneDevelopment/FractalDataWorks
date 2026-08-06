using System.Text.Json.Serialization;
using Fdw.Services.Abstractions.Health.Converters;

namespace Fdw.Services.Abstractions.Health;

/// <summary>
/// Represents a health state for services following the TypeCollection pattern.
/// </summary>
/// <remarks>
/// Health states are extensible TypeOptions that indicate service health status.
/// Common states: Healthy, Unhealthy, Degraded.
/// </remarks>
// Why: applying the converter on the interface makes every IHealthState-typed property
// (de)serialize correctly wherever it appears, on the wire or on disk, without per-call wiring.
[JsonConverter(typeof(HealthStateJsonConverter))]
public interface IHealthState
{
    /// <summary>
    /// Gets the unique identifier for this health state.
    /// </summary>
    /// <value>Unique integer ID for the health state.</value>
    int Id { get; }

    /// <summary>
    /// Gets the name of this health state.
    /// </summary>
    /// <value>Name such as "Healthy", "Unhealthy", or "Degraded".</value>
    string Name { get; }

    /// <summary>
    /// Gets whether this represents a healthy state.
    /// </summary>
    /// <value>True if the service is healthy, false otherwise.</value>
    bool IsHealthy { get; }
}
