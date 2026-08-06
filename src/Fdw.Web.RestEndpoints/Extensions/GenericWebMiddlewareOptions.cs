namespace Fdw.Web.RestEndpoints.Extensions;

/// <summary>
/// Options for configuring the Fdw Web Framework middleware.
/// </summary>
public sealed class GenericWebMiddlewareOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether request validation is enabled.
    /// </summary>
    public bool EnableRequestValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether security headers are enabled.
    /// </summary>
    public bool EnableSecurityHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether rate limiting is enabled.
    /// </summary>
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether authentication is enabled.
    /// </summary>
    public bool EnableAuthentication { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether authorization is enabled.
    /// </summary>
    public bool EnableAuthorization { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether CORS is enabled.
    /// </summary>
    public bool EnableCors { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether exception handling is enabled.
    /// </summary>
    public bool EnableExceptionHandling { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether request/response logging is enabled.
    /// </summary>
    public bool EnableRequestResponseLogging { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether performance monitoring is enabled.
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether health checks are enabled.
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Gets or sets the order in which middleware should be added.
    /// Allows for custom middleware ordering if needed.
    /// </summary>
    public string[] MiddlewareOrder { get; set; } =
    [
        "ExceptionHandling",
        "SecurityHeaders",
        "Cors",
        "RequestValidation",
        "Authentication",
        "Authorization",
        "RateLimiting",
        "PerformanceMonitoring",
        "RequestResponseLogging",
        "Endpoints",
        "HealthChecks"
    ];

    /// <summary>
    /// Gets or sets custom middleware types to include in the pipeline.
    /// </summary>
    public Type[] CustomMiddleware { get; set; } = [];

    /// <summary>
    /// Gets or sets the position where custom middleware should be inserted.
    /// </summary>
    public string CustomMiddlewarePosition { get; set; } = "BeforeEndpoints";
}