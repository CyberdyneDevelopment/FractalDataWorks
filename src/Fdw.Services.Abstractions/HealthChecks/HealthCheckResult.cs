using System;
using System.Collections.Generic;

namespace Fdw.Services.Abstractions.Health;

/// <summary>
/// Default mutable implementation of <see cref="IHealthCheckResult"/>.
/// </summary>
/// <remarks>
/// Every <see cref="IHealthCheckable"/> implementation constructs one of these to return from
/// <see cref="IHealthCheckable.CheckHealth"/>. Kept as a plain settable POCO (matching
/// <c>Fdw.Services.Abstractions.Health.Monitoring.ServiceHealthSnapshot</c> and
/// <c>HealthCheckPoint</c>) so callers can populate exactly the fields they have.
/// </remarks>
public sealed class HealthCheckResult : IHealthCheckResult
{
    /// <inheritdoc/>
    // Why: required, no initializer — a checkable that forgets to set Status must not
    // silently report Healthy (no-fallbacks doctrine).
    public required IHealthState Status { get; set; }

    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public Exception? Exception { get; set; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object> Data { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <inheritdoc/>
    public TimeSpan Duration { get; set; }
}
