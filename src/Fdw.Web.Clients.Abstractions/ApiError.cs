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
    /// Well-known error type URIs, each <see cref="BaseUri"/> plus a fixed slug.
    /// </summary>
    public static class Types
    {
        /// <summary>
        /// The URI every error type below starts with. A deployment that documents its errors
        /// under its own domain sets this before the first error is built.
        /// </summary>
        public static string BaseUri { get; set; } = "https://fdw.dev/errors/";

        /// <summary>Validation error.</summary>
        public static string Validation => BaseUri + "validation";

        /// <summary>Resource not found.</summary>
        public static string NotFound => BaseUri + "not-found";

        /// <summary>Resource conflict (e.g., concurrent modification).</summary>
        public static string Conflict => BaseUri + "conflict";

        /// <summary>Authentication required.</summary>
        public static string Unauthorized => BaseUri + "unauthorized";

        /// <summary>Access denied.</summary>
        public static string Forbidden => BaseUri + "forbidden";

        /// <summary>Rate limit exceeded.</summary>
        public static string RateLimited => BaseUri + "rate-limited";

        /// <summary>Internal server error.</summary>
        public static string ServerError => BaseUri + "server-error";
    }
}
