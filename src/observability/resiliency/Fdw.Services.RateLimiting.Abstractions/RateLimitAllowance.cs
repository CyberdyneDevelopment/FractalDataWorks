using System;
using System.Globalization;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// What one request is held to under a rate limit policy: the numbers and the names of the algorithm and of the way requests
/// are counted, as the platform's settings give them for the request's caller.
/// </summary>
/// <remarks>
/// <para>
/// It is built for each request from settings and never held on the policy: a policy is only the name of a tier
/// (FDW-1945). Nothing here has a default; every member is what a setting said.
/// </para>
/// <para>
/// <see cref="Fingerprint"/> is part of the bucket's key, so a setting that changes starts a new bucket under the new
/// numbers and the old one is left to age out. A limiter is never edited in place.
/// </para>
/// </remarks>
public sealed class RateLimitAllowance
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitAllowance"/> class.
    /// </summary>
    /// <param name="requestsPerWindow">The most requests allowed in a window.</param>
    /// <param name="window">How long a window lasts.</param>
    /// <param name="allowBurst">Whether requests above <paramref name="requestsPerWindow"/> are allowed up to <paramref name="burstLimit"/>.</param>
    /// <param name="burstLimit">The most requests a burst allows.</param>
    /// <param name="algorithm">The algorithm that counts requests.</param>
    /// <param name="segmentsPerWindow">The segments a sliding window is cut into.</param>
    /// <param name="queueExceededRequests">Whether requests over the limit wait instead of being refused.</param>
    /// <param name="queueLimit">The most requests that wait.</param>
    /// <param name="partitioning">How requests are divided into buckets.</param>
    public RateLimitAllowance(
        int requestsPerWindow,
        TimeSpan window,
        bool allowBurst,
        int burstLimit,
        IRateLimitAlgorithm algorithm,
        int segmentsPerWindow,
        bool queueExceededRequests,
        int queueLimit,
        IRateLimitPartitioning partitioning)
    {
        RequestsPerWindow = requestsPerWindow;
        Window = window;
        AllowBurst = allowBurst;
        BurstLimit = burstLimit;
        Algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
        SegmentsPerWindow = segmentsPerWindow;
        QueueExceededRequests = queueExceededRequests;
        QueueLimit = queueLimit;
        Partitioning = partitioning ?? throw new ArgumentNullException(nameof(partitioning));
    }

    /// <summary>
    /// Gets the maximum number of requests allowed within the time window.
    /// </summary>
    public int RequestsPerWindow { get; }

    /// <summary>
    /// Gets the duration of the time window for rate limiting.
    /// </summary>
    public TimeSpan Window { get; }

    /// <summary>
    /// Gets a value indicating whether burst requests above the normal limit are allowed.
    /// When enabled, temporary spikes up to <see cref="BurstLimit"/> are permitted.
    /// </summary>
    public bool AllowBurst { get; }

    /// <summary>
    /// Gets the maximum burst limit when <see cref="AllowBurst"/> is enabled.
    /// </summary>
    public int BurstLimit { get; }

    /// <summary>
    /// Gets the rate limiting algorithm.
    /// </summary>
    public IRateLimitAlgorithm Algorithm { get; }

    /// <summary>
    /// Gets the number of segments per window for sliding window algorithms.
    /// </summary>
    public int SegmentsPerWindow { get; }

    /// <summary>
    /// Gets a value indicating whether requests that exceed the limit are queued rather than immediately rejected.
    /// </summary>
    public bool QueueExceededRequests { get; }

    /// <summary>
    /// Gets the maximum number of requests that can be queued when limits are exceeded.
    /// </summary>
    public int QueueLimit { get; }

    /// <summary>
    /// Gets how requests are divided into buckets.
    /// </summary>
    public IRateLimitPartitioning Partitioning { get; }

    /// <summary>
    /// Gets a text that is the same for two allowances exactly when every number and name in them is the same.
    /// </summary>
    public string Fingerprint => string.Join(
        "/",
        RequestsPerWindow.ToString(CultureInfo.InvariantCulture),
        Window.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
        AllowBurst ? "burst" : "noburst",
        BurstLimit.ToString(CultureInfo.InvariantCulture),
        Algorithm.Name,
        SegmentsPerWindow.ToString(CultureInfo.InvariantCulture),
        QueueExceededRequests ? "queue" : "noqueue",
        QueueLimit.ToString(CultureInfo.InvariantCulture),
        Partitioning.Name);
}
