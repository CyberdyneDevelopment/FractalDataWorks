using Fdw.Collections;

namespace Fdw.Web.Http.Abstractions.Policies;

/// <summary>
/// Enhanced enum defining rate limiting policies for endpoint protection.
/// Provides various strategies for controlling request frequency and preventing abuse.
/// </summary>
public abstract class RateLimitPolicyBase : TypeOptionBase<int, IRateLimitPolicy>, IRateLimitPolicy
{
    /// <summary>
    /// Gets the maximum number of requests allowed within the time window.
    /// </summary>
    public int MaxRequests { get; }

    /// <summary>
    /// Gets the time window duration in seconds for rate limiting.
    /// </summary>
    public int WindowSizeInSeconds { get; }

    /// <summary>
    /// Gets the policy type identifier for the rate limiting algorithm.
    /// </summary>
    public string PolicyType { get; }

    /// <summary>
    /// Gets a value indicating whether rate limiting is enabled for this policy.
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Gets the default request limit per time window.
    /// Returns null if not applicable to this policy type.
    /// </summary>
    public int? DefaultRequestLimit { get; }

    /// <summary>
    /// Gets the default time window in seconds.
    /// Returns null if not applicable to this policy type.
    /// </summary>
    public int? DefaultTimeWindowSeconds { get; }

    /// <summary>
    /// Gets a value indicating whether this policy supports burst capacity.
    /// </summary>
    public bool SupportsBurstCapacity { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitPolicyBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this rate limit policy.</param>
    /// <param name="name">The name of the rate limit policy.</param>
    /// <param name="maxRequests">The maximum number of requests allowed within the time window.</param>
    /// <param name="windowSizeInSeconds">The time window duration in seconds for rate limiting.</param>
    /// <param name="policyType">The policy type identifier for the rate limiting algorithm.</param>
    /// <param name="isEnabled">Indicates whether rate limiting is enabled for this policy.</param>
    /// <param name="defaultRequestLimit">The default request limit per time window.</param>
    /// <param name="defaultTimeWindowSeconds">The default time window in seconds.</param>
    /// <param name="supportsBurstCapacity">Indicates whether this policy supports burst capacity.</param>
    protected RateLimitPolicyBase(
        int id,
        string name,
        int maxRequests,
        int windowSizeInSeconds,
        string policyType,
        bool isEnabled,
        int? defaultRequestLimit,
        int? defaultTimeWindowSeconds,
        bool supportsBurstCapacity)
        : base(id, name)
    {
        MaxRequests = maxRequests;
        WindowSizeInSeconds = windowSizeInSeconds;
        PolicyType = policyType;
        IsEnabled = isEnabled;
        DefaultRequestLimit = defaultRequestLimit;
        DefaultTimeWindowSeconds = defaultTimeWindowSeconds;
        SupportsBurstCapacity = supportsBurstCapacity;
    }
}
