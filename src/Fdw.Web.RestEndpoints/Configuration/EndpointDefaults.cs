using System;
using Fdw.Web.Http.Abstractions.EndPoints;
using Fdw.Web.Http.Abstractions.Policies;
using Fdw.Web.Http.Abstractions.Security;

namespace Fdw.Web.RestEndpoints.Configuration;

/// <summary>
/// Default configuration values for endpoints.
/// Provides centralized management of endpoint behavior defaults.
/// </summary>
public static class EndpointDefaults
{
    /// <summary>
    /// The default security method for endpoints when none is specified.
    /// </summary>
    public static ISecurityMethod DefaultSecurityMethod => SecurityMethods.None;

    /// <summary>
    /// The default rate limit policy for endpoints when none is specified.
    /// </summary>
    public static IRateLimitPolicy DefaultRateLimitPolicy => RateLimitPolicies.None;

    /// <summary>
    /// The default request timeout in milliseconds.
    /// </summary>
    public const int DefaultRequestTimeoutMs = 30000; // 30 seconds

    /// <summary>
    /// The default maximum request body size in bytes.
    /// </summary>
    public const long DefaultMaxRequestBodySize = 10485760; // 10MB

    /// <summary>
    /// The default rate limit window in seconds.
    /// </summary>
    public const int DefaultRateLimitWindowSeconds = 60;

    /// <summary>
    /// The default maximum requests per rate limit window.
    /// </summary>
    public const int DefaultRateLimitMaxRequests = 100;

    /// <summary>
    /// The default cache duration in seconds for cacheable responses.
    /// </summary>
    public const int DefaultCacheDurationSeconds = 300; // 5 minutes

    /// <summary>
    /// The default content type for API responses.
    /// </summary>
    public const string DefaultContentType = "application/json";

    /// <summary>
    /// The default API version when none is specified.
    /// </summary>
    public const string DefaultApiVersion = "v1";

    /// <summary>
    /// The default endpoint timeout for long-running operations.
    /// </summary>
    public static TimeSpan DefaultOperationTimeout => TimeSpan.FromMinutes(5);

    /// <summary>
    /// Default HTTP status code for successful operations.
    /// </summary>
    public const int DefaultSuccessStatusCode = 200;

    /// <summary>
    /// Default HTTP status code for validation errors.
    /// </summary>
    public const int DefaultGenericValidationErrorStatusCode = 400;

    /// <summary>
    /// Default HTTP status code for authentication failures.
    /// </summary>
    public const int DefaultAuthenticationErrorStatusCode = 401;

    /// <summary>
    /// Default HTTP status code for authorization failures.
    /// </summary>
    public const int DefaultAuthorizationErrorStatusCode = 403;

    /// <summary>
    /// Default HTTP status code for resource not found.
    /// </summary>
    public const int DefaultNotFoundStatusCode = 404;

    /// <summary>
    /// Default HTTP status code for rate limit exceeded.
    /// </summary>
    public const int DefaultRateLimitStatusCode = 429;

    /// <summary>
    /// Default HTTP status code for server errors.
    /// </summary>
    public const int DefaultServerErrorStatusCode = 500;

    /// <summary>
    /// Default headers to include in all responses.
    /// </summary>
    public static readonly string[] DefaultResponseHeaders =
    [
        "X-Content-Type-Options: nosniff",
        "X-Frame-Options: DENY",
        "X-XSS-Protection: 1; mode=block"
    ];

    /// <summary>
    /// Default CORS headers when CORS is enabled.
    /// </summary>
    public static readonly string[] DefaultCorsHeaders =
    [
        "Content-Type",
        "Authorization",
        "X-API-Key",
        "X-Requested-With"
    ];

    /// <summary>
    /// Gets default endpoint settings for a specific endpoint type.
    /// </summary>
    /// <param name="endpointType">The type of endpoint to get defaults for.</param>
    /// <returns>A configuration object with appropriate defaults.</returns>
    public static EndpointDefaultSettings GetDefaultsForType(IEndpointType endpointType)
    {
        return new EndpointDefaultSettings(
            endpointType.SecurityMethodName,
            endpointType.RateLimitPolicyName,
            endpointType.TimeoutMs,
            endpointType.MaxBodySize,
            endpointType.RequiresAuthentication,
            endpointType.AllowedRoles);
    }
}