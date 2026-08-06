using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Quality.Logging;

/// <summary>
/// MessageLogging methods for Promotion operations.
/// Every log message is returned in the result AND logged.
/// EventId range: 8450-8499
/// </summary>
[MessageLoggingTypeCode("QUALITY")]
public static partial class PromotionLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Request Events (8450-8459)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs creation of a promotion request.</summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Information,
        Message = "Promotion request created: {sourceEnvironment} → {targetEnvironment} by '{requestedBy}'")]
    public static partial IGenericMessage RequestCreated(ILogger logger, string sourceEnvironment, string targetEnvironment, string requestedBy);

    /// <summary>Logs approval of a promotion request.</summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Information,
        Message = "Promotion request {requestId} approved by '{approvedBy}'")]
    public static partial IGenericMessage RequestApproved(ILogger logger, Guid requestId, string approvedBy);

    /// <summary>Logs rejection of a promotion request.</summary>
    [MessageLogging(EventId = 41001, Level = LogLevel.Warning,
        Message = "Promotion request {requestId} rejected by '{rejectedBy}': {reason}")]
    public static partial IGenericMessage RequestRejected(ILogger logger, Guid requestId, string rejectedBy, string reason);

    /// <summary>Logs loading a promotion request.</summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Debug,
        Message = "Loading promotion request {requestId}")]
    public static partial IGenericMessage LoadingRequest(ILogger logger, Guid requestId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Execution Events (8460-8469)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs the start of a promotion.</summary>
    [MessageLogging(EventId = 11012, Level = LogLevel.Information,
        Message = "Promotion {requestId} started: {itemCount} items to promote")]
    public static partial IGenericMessage PromotionStarted(ILogger logger, Guid requestId, int itemCount);

    /// <summary>Logs completion of a promotion.</summary>
    [MessageLogging(EventId = 11013, Level = LogLevel.Information,
        Message = "Promotion {requestId} completed: {itemCount} items promoted")]
    public static partial IGenericMessage PromotionCompleted(ILogger logger, Guid requestId, int itemCount);

    /// <summary>Logs promoting a specific item.</summary>
    [MessageLogging(EventId = 11014, Level = LogLevel.Debug,
        Message = "Promoting item '{itemType}:{itemName}' to {targetEnvironment}")]
    public static partial IGenericMessage PromotingItem(ILogger logger, string itemType, string itemName, string targetEnvironment);

    /// <summary>Logs successful promotion of an item.</summary>
    [MessageLogging(EventId = 11015, Level = LogLevel.Information,
        Message = "Item '{itemType}:{itemName}' promoted successfully")]
    public static partial IGenericMessage ItemPromoted(ILogger logger, string itemType, string itemName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Comparison Events (8470-8479)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs the start of an environment comparison.</summary>
    [MessageLogging(EventId = 11016, Level = LogLevel.Debug,
        Message = "Comparing environments: {sourceEnvironment} vs {targetEnvironment}")]
    public static partial IGenericMessage ComparingEnvironments(ILogger logger, string sourceEnvironment, string targetEnvironment);

    /// <summary>Logs completion of an environment comparison.</summary>
    [MessageLogging(EventId = 11017, Level = LogLevel.Information,
        Message = "Environment comparison completed: {addedCount} added, {modifiedCount} modified, {removedCount} removed")]
    public static partial IGenericMessage ComparisonCompleted(ILogger logger, int addedCount, int modifiedCount, int removedCount);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error Events (8490-8499)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs that a promotion request was not found.</summary>
    [MessageLogging(EventId = 31002, Level = LogLevel.Error,
        Message = "Promotion request not found: {requestId}")]
    public static partial IGenericMessage RequestNotFound(ILogger logger, Guid requestId);

    /// <summary>Logs that a promotion request is not approved.</summary>
    [MessageLogging(EventId = 41002, Level = LogLevel.Error,
        Message = "Cannot promote: request {requestId} is not approved (status: {status})")]
    public static partial IGenericMessage NotApproved(ILogger logger, Guid requestId, string status);

    /// <summary>Logs that an environment was not found.</summary>
    [MessageLogging(EventId = 31003, Level = LogLevel.Error,
        Message = "Environment not found: '{environmentName}'")]
    public static partial IGenericMessage EnvironmentNotFound(ILogger logger, string environmentName);

    /// <summary>Logs a promotion failure.</summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error,
        Message = "Promotion {requestId} failed")]
    public static partial IGenericMessage PromotionFailed(ILogger logger, Exception exception, Guid requestId);

    /// <summary>Logs a failed item promotion.</summary>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error,
        Message = "Item promotion failed for '{itemType}:{itemName}': {error}")]
    public static partial IGenericMessage ItemPromotionFailed(ILogger logger, string itemType, string itemName, string error);

    /// <summary>Logs an attempt to promote to the same environment.</summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error,
        Message = "Cannot promote to same environment: {environmentName}")]
    public static partial IGenericMessage SameEnvironmentError(ILogger logger, string environmentName);
}
