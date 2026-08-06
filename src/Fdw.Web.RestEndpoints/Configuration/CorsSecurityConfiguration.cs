using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.RestEndpoints.Configuration;

/// <summary>
/// CORS security configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CorsSecurityConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether CORS is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets or sets the allowed origins for CORS requests.
    /// </summary>
    public string[] AllowedOrigins { get; init; } = [];

    /// <summary>
    /// Gets or sets the allowed methods for CORS requests.
    /// </summary>
    public string[] AllowedMethods { get; init; } = ["GET", "POST", "PUT", "DELETE"];

    /// <summary>
    /// Gets or sets the allowed headers for CORS requests.
    /// </summary>
    public string[] AllowedHeaders { get; init; } = ["Content-Type", "Authorization"];

    /// <summary>
    /// Gets or sets a value indicating whether credentials are allowed for CORS requests.
    /// </summary>
    public bool AllowCredentials { get; init; }

    /// <summary>
    /// Gets or sets the maximum age for CORS preflight requests in seconds.
    /// </summary>
    public int PreflightMaxAgeSeconds { get; init; } = 86400; // 24 hours
}