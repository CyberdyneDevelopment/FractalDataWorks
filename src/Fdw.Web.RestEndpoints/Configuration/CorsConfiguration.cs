using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.RestEndpoints.Configuration;

/// <summary>
/// CORS configuration for cross-origin requests.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CorsConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether CORS is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets or sets the allowed origins for CORS.
    /// </summary>
    public string[] AllowedOrigins { get; init; } = ["*"];

    /// <summary>
    /// Gets or sets the allowed HTTP methods for CORS.
    /// </summary>
    public string[] AllowedMethods { get; init; } = ["GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS"];

    /// <summary>
    /// Gets or sets the allowed headers for CORS.
    /// </summary>
    public string[] AllowedHeaders { get; init; } = ["*"];

    /// <summary>
    /// Gets or sets a value indicating whether credentials are allowed for CORS.
    /// </summary>
    public bool AllowCredentials { get; init; }
}