using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.RateLimiting.Logging;

/// <summary>
/// MessageLogging methods for rate limiting operations.
/// Every log message is returned in the result AND logged.
/// </summary>
[MessageLoggingTypeCode("RATELIMITING")]
public static partial class RateLimitLog
{
    // ===============================================================================
    // Rate Limit Events (6201-6210)
    // ===============================================================================

    /// <summary>
    /// Logs when a rate limit is exceeded for a client.
    /// </summary>
    [MessageLogging(
        EventId = 81000,
        Level = LogLevel.Warning,
        Message = "Rate limit exceeded for client '{clientId}' on policy '{policyName}', requests: {requestCount}/{limit}")]
    public static partial IGenericMessage RateLimitExceeded(
        ILogger logger,
        string clientId,
        string policyName,
        int requestCount,
        int limit);

    /// <summary>
    /// Logs when a rate limit is applied to a request.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Information,
        Message = "Rate limit applied for policy '{policyName}': {requestCount}/{limit} requests in window")]
    public static partial IGenericMessage RateLimitApplied(
        ILogger logger,
        string policyName,
        int requestCount,
        int limit);

    /// <summary>
    /// Logs when a rate limiter is configured for a policy.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "Rate limiter configured for policy '{policyName}': {limit} requests per {windowSeconds}s")]
    public static partial IGenericMessage RateLimiterConfigured(
        ILogger logger,
        string policyName,
        int limit,
        int windowSeconds);

    /// <summary>
    /// Logs when a request is queued due to rate limiting.
    /// </summary>
    [MessageLogging(
        EventId = 81001,
        Level = LogLevel.Warning,
        Message = "Client '{clientId}' queued for rate limit on policy '{policyName}', queue position: {position}")]
    public static partial IGenericMessage RequestQueued(
        ILogger logger,
        string clientId,
        string policyName,
        int position);

    // ===============================================================================
    // Policy Registration Events (6211-6220)
    // ===============================================================================

    /// <summary>
    /// Logs when a rate limit policy is registered.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Information,
        Message = "Rate limit policy '{policyName}' registered with algorithm '{algorithm}'")]
    public static partial IGenericMessage PolicyRegistered(
        ILogger logger,
        string policyName,
        string algorithm);

    /// <summary>
    /// Logs when a rate limit policy is not found.
    /// </summary>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Error,
        Message = "Rate limit policy '{policyName}' not found")]
    public static partial IGenericMessage PolicyNotFound(
        ILogger logger,
        string policyName);

    /// <summary>
    /// Logs when all rate limit policies are registered.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "Registered {policyCount} rate limit policies from TypeCollection")]
    public static partial IGenericMessage AllPoliciesRegistered(
        ILogger logger,
        int policyCount);

    // ===============================================================================
    // Rejection Events (6221-6230)
    // ===============================================================================

    /// <summary>
    /// Logs when the Retry-After header is set for a rejected request.
    /// </summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Debug,
        Message = "Retry-After header set to {retryAfterSeconds}s for client '{clientId}'")]
    public static partial IGenericMessage RetryAfterSet(
        ILogger logger,
        int retryAfterSeconds,
        string clientId);

    /// <summary>
    /// Logs when a request is rejected due to rate limiting.
    /// </summary>
    [MessageLogging(
        EventId = 81002,
        Level = LogLevel.Warning,
        Message = "Request rejected for client '{clientId}', returning 429 Too Many Requests")]
    public static partial IGenericMessage RequestRejected(
        ILogger logger,
        string clientId);

    /// <summary>
    /// Logs when writing the rate limit rejection response.
    /// </summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Debug,
        Message = "Writing rate limit rejection response for client '{clientId}'")]
    public static partial IGenericMessage WritingRejectionResponse(
        ILogger logger,
        string clientId);

    // ===============================================================================
    // Error Events (6231-6240)
    // ===============================================================================

    /// <summary>
    /// Logs when an exception occurs during rate limit handling.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Exception during rate limit handling for client '{clientId}'")]
    public static partial IGenericMessage RateLimitHandlingException(
        ILogger logger,
        Exception exception,
        string clientId);

    /// <summary>
    /// Logs when policy registration fails.
    /// </summary>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Error,
        Message = "Failed to register rate limit policy '{policyName}': {error}")]
    public static partial IGenericMessage PolicyRegistrationFailed(
        ILogger logger,
        string policyName,
        string error);

    // ===============================================================================
    // Diagnostic Events (6241-6250)
    // ===============================================================================

    /// <summary>
    /// Logs detailed policy configuration for debugging.
    /// </summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Debug,
        Message = "Policy '{policyName}' configuration - RequestsPerWindow: {requestsPerWindow}, Window: {windowMs}ms, Algorithm: {algorithm}, AllowBurst: {allowBurst}, BurstLimit: {burstLimit}")]
    public static partial IGenericMessage PolicyConfiguration(
        ILogger logger,
        string policyName,
        int requestsPerWindow,
        double windowMs,
        string algorithm,
        bool allowBurst,
        int burstLimit);

    /// <summary>
    /// Logs when the rate limiting middleware is initialized.
    /// </summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Information,
        Message = "Rate limiting middleware initialized with {policyCount} policies")]
    public static partial IGenericMessage MiddlewareInitialized(
        ILogger logger,
        int policyCount);
}
