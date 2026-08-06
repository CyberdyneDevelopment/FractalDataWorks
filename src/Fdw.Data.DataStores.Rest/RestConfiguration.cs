using Fdw.Configuration;

namespace Fdw.Data.DataStores.Rest;

/// <summary>
/// Configuration for REST API data store.
/// </summary>
public sealed class RestConfiguration
{
    /// <summary>
    /// Gets or sets the base URL for the REST API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OpenAPI specification URL or file path.
    /// </summary>
    public string? OpenApiSpecUrl { get; set; }

    /// <summary>
    /// Gets or sets the authentication type (Bearer, ApiKey, Basic, etc.).
    /// </summary>
    public string? AuthenticationType { get; set; }

    /// <summary>
    /// Gets or sets the API key for API Key authentication.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the bearer token for Bearer authentication.
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// Gets or sets the request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to retry failed requests.
    /// </summary>
    public bool EnableRetries { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}
