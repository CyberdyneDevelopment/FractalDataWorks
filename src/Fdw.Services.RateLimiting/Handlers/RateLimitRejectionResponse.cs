namespace Fdw.Services.RateLimiting.Handlers;

/// <summary>
/// Response body for rate limit rejection responses.
/// </summary>
/// <remarks>
/// This class is serialized to JSON and returned in the response body
/// when a request is rejected due to rate limiting.
/// </remarks>
public sealed class RateLimitRejectionResponse
{
    /// <summary>
    /// Gets or sets the error type identifier.
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a human-readable message describing the error.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of seconds after which the client can retry.
    /// </summary>
    public int RetryAfterSeconds { get; set; }
}