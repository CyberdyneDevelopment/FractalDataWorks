using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.RestEndpoints.Configuration;

/// <summary>
/// API key authentication configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ApiKeyConfiguration
{
    /// <summary>
    /// Gets or sets the header name for API key authentication.
    /// </summary>
    public string HeaderName { get; init; } = "X-API-Key";

    /// <summary>
    /// Gets or sets the valid API keys.
    /// In production, these should be stored securely.
    /// </summary>
    public string[] ValidKeys { get; init; } = [];
}