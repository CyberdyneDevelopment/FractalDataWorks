using System.Collections.Generic;

namespace Fdw.Web.Clients.Abstractions;

/// <summary>
/// Standardized API error response following RFC 7807 Problem Details.
/// </summary>
public sealed class ApiError
{
    /// <summary>
    /// Gets the URI reference identifying the problem type.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the short human-readable summary.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public required int Status { get; init; }

    /// <summary>
    /// Gets the human-readable explanation specific to this occurrence.
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>
    /// Gets the URI reference identifying the specific occurrence.
    /// </summary>
    public string? Instance { get; init; }

    /// <summary>
    /// Gets the correlation ID for tracing.
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// Gets field-specific validation errors.
    /// </summary>
    public IDictionary<string, string[]>? Errors { get; init; }

    /// <summary>
    /// Gets the error code for programmatic handling.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Well-known error type URIs.
    /// </summary>
    public static class Types
    {
        /// <summary>Validation error.</summary>
        public const string Validation = "https://fdw.dev/errors/validation";

        /// <summary>Resource not found.</summary>
        public const string NotFound = "https://fdw.dev/errors/not-found";

        /// <summary>Resource conflict (e.g., concurrent modification).</summary>
        public const string Conflict = "https://fdw.dev/errors/conflict";

        /// <summary>Authentication required.</summary>
        public const string Unauthorized = "https://fdw.dev/errors/unauthorized";

        /// <summary>Access denied.</summary>
        public const string Forbidden = "https://fdw.dev/errors/forbidden";

        /// <summary>Rate limit exceeded.</summary>
        public const string RateLimited = "https://fdw.dev/errors/rate-limited";

        /// <summary>Internal server error.</summary>
        public const string ServerError = "https://fdw.dev/errors/server-error";
    }
}
