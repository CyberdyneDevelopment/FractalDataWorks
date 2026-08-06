using System;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Resiliency.Abstractions.Policies;

/// <summary>
/// Resiliency policy optimized for HTTP client operations.
/// Designed to handle network-related transient failures, rate limiting,
/// and temporary service unavailability scenarios.
/// </summary>
/// <remarks>
/// <para>
/// This policy uses higher retry counts than database operations due to the
/// inherently unreliable nature of network communications. The exponential
/// backoff helps respect rate limits and allows remote services time to recover.
/// </para>
/// <para>
/// Circuit breaker settings are calibrated for HTTP scenarios where a remote
/// service being down should not cause cascade failures in the calling application.
/// </para>
/// </remarks>
[TypeOption(typeof(ResiliencyPolicies), "HttpClient", RestrictToCurrentCompilation = true)]
public sealed class HttpClientResiliencyPolicy : ResiliencyPolicyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpClientResiliencyPolicy"/> class.
    /// </summary>
    public HttpClientResiliencyPolicy() : base(2, "HttpClient")
    {
    }

    /// <summary>
    /// Gets the maximum number of retry attempts (5).
    /// </summary>
    public override int MaxRetries => 5;

    /// <summary>
    /// Gets the initial delay before the first retry (200ms).
    /// </summary>
    public override TimeSpan InitialDelay => TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Gets the maximum delay between retries (30 seconds).
    /// </summary>
    public override TimeSpan MaxDelay => TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the backoff multiplier (2.0 - doubles delay each retry).
    /// </summary>
    public override double BackoffMultiplier => 2.0;

    /// <summary>
    /// Gets the circuit breaker open duration (60 seconds).
    /// </summary>
    public override TimeSpan CircuitBreakerDuration => TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets the number of failures to trip the circuit breaker (10).
    /// </summary>
    public override int CircuitBreakerThreshold => 10;

    /// <summary>
    /// Gets the resiliency category (HttpClient).
    /// </summary>
    public override IResiliencyCategory ResiliencyCategory => ResiliencyCategories.HttpClient;
}
