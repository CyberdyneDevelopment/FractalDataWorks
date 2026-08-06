using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Web.RestEndpoints.Security;

/// <summary>
/// MessageLogging for security-tier endpoint configuration and observability.
/// EventId range: 6400-6420
/// </summary>
[MessageLoggingTypeCode("RESTENDPOINTS")]
public static partial class SecurityEndpointLog
{
    /// <summary>
    /// Logs when a security-tier endpoint is configured during startup.
    /// </summary>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Trace,
        Message = "Endpoint '{endpointName}' configured with security tier '{securityTier}' at route '{route}'")]
    public static partial IGenericMessage EndpointConfigured(
        ILogger logger,
        string endpointName,
        string securityTier,
        string route);

    /// <summary>
    /// Logs when a rate limit policy is applied to an endpoint.
    /// </summary>
    [MessageLogging(
        EventId = 11016,
        Level = LogLevel.Debug,
        Message = "Rate limit policy '{policyName}' applied to endpoint '{endpointName}'")]
    public static partial IGenericMessage RateLimitPolicyApplied(
        ILogger logger,
        string endpointName,
        string policyName);

    /// <summary>
    /// Logs when an authorization policy is applied to an endpoint.
    /// </summary>
    [MessageLogging(
        EventId = 11017,
        Level = LogLevel.Debug,
        Message = "Authorization policy '{policyName}' applied to endpoint '{endpointName}'")]
    public static partial IGenericMessage AuthorizationPolicyApplied(
        ILogger logger,
        string endpointName,
        string policyName);

    /// <summary>
    /// Logs when an endpoint is configured for anonymous access.
    /// </summary>
    [MessageLogging(
        EventId = 11018,
        Level = LogLevel.Information,
        Message = "Anonymous access configured for endpoint '{endpointName}' at route '{route}'")]
    public static partial IGenericMessage AnonymousAccessConfigured(
        ILogger logger,
        string endpointName,
        string route);

    // Why: authorization is being DISABLED for this endpoint — an abnormal-but-handled state that
    // should be visible above Info, not blend in with routine startup announcements.
    /// <summary>
    /// Logs when development mode bypasses authentication/authorization for an endpoint.
    /// </summary>
    [MessageLogging(
        EventId = 11019,
        Level = LogLevel.Warning,
        Message = "Development mode bypass active for endpoint '{endpointName}'")]
    public static partial IGenericMessage DevelopmentModeBypassActive(
        ILogger logger,
        string endpointName);
}
