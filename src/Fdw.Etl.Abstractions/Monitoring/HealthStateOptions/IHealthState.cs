using Fdw.Collections;
using Fdw.Etl.Abstractions.Monitoring.HealthStateOptions.Converters;
using System.Text.Json.Serialization;

namespace Fdw.Etl.Abstractions.Monitoring.HealthStateOptions;

/// <summary>
/// Interface for health states.
/// Extends ITypeOption to enable TypeCollection discovery.
/// </summary>
// Why: applying the converter on the interface makes every IHealthState-typed property
// (de)serialize correctly wherever it appears, on the wire or on disk, without per-call wiring.
[JsonConverter(typeof(HealthStateJsonConverter))]
public interface IHealthState : ITypeOption<int, HealthStateBase>
{
    /// <summary>
    /// Gets a value indicating whether this state represents a healthy condition.
    /// </summary>
    bool IsHealthy { get; }

    /// <summary>
    /// Gets a value indicating whether this state requires attention.
    /// </summary>
    bool RequiresAttention { get; }
}
