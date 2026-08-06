using Fdw.Collections;

namespace Fdw.Web.Http.Abstractions.Policies;

/// <summary>
/// Interface for rate limit policy type options.
/// Provides abstraction for rate limiting strategies.
/// </summary>
public interface IRateLimitPolicy : ITypeOption<int, IRateLimitPolicy>
{
    /// <summary>
    /// Gets a value indicating whether rate limiting is enabled for this policy.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the default request limit per time window.
    /// Returns null if not applicable to this policy type.
    /// </summary>
    int? DefaultRequestLimit { get; }

    /// <summary>
    /// Gets the default time window in seconds.
    /// Returns null if not applicable to this policy type.
    /// </summary>
    int? DefaultTimeWindowSeconds { get; }

    /// <summary>
    /// Gets a value indicating whether this policy supports burst capacity.
    /// </summary>
    bool SupportsBurstCapacity { get; }
}
