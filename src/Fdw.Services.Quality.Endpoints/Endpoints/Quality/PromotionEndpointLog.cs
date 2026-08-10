using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>
/// MessageLogging definitions for Promotion endpoint base classes.
/// EventId range: 7300-7349
/// </summary>
[MessageLoggingTypeCode("QUALITYENDPOINTS")]
public static partial class PromotionEndpointLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // List Operations (7300-7309)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs the start of listing environments.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace, Message = "Listing deployment environments")]
    public static partial IGenericMessage ListingEnvironments(ILogger logger);

    /// <summary>Logs the number of environments found.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information, Message = "Found {count} deployment environments")]
    public static partial IGenericMessage EnvironmentsFound(ILogger logger, int count);

    /// <summary>Logs a failure to list environments.</summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Failed to list deployment environments: {message}")]
    public static partial IGenericMessage ListEnvironmentsFailed(ILogger logger, string message);

    /// <summary>Logs the start of listing promotion requests.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace, Message = "Listing promotion requests")]
    public static partial IGenericMessage ListingPromotions(ILogger logger);

    /// <summary>Logs the number of promotion requests found.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Information, Message = "Found {count} promotion requests")]
    public static partial IGenericMessage PromotionsFound(ILogger logger, int count);

    /// <summary>Logs a failure to list promotion requests.</summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error, Message = "Failed to list promotion requests: {message}")]
    public static partial IGenericMessage ListPromotionsFailed(ILogger logger, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Get Operations (7310-7314)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs the start of fetching a promotion request.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace, Message = "Getting promotion request {requestId}")]
    public static partial IGenericMessage GettingPromotion(ILogger logger, Guid requestId);

    /// <summary>Logs that a promotion request was not found.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning, Message = "Promotion request {requestId} not found")]
    public static partial IGenericMessage PromotionNotFound(ILogger logger, Guid requestId);

    /// <summary>Logs a failure to get a promotion request.</summary>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error, Message = "Failed to get promotion request {requestId}: {message}")]
    public static partial IGenericMessage GetPromotionFailed(ILogger logger, Guid requestId, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Create Operations (7315-7319)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs the start of creating a promotion request.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace, Message = "Creating promotion: {sourceEnvironment} to {targetEnvironment} by '{requestedBy}'")]
    public static partial IGenericMessage CreatingPromotion(ILogger logger, string sourceEnvironment, string targetEnvironment, string requestedBy);

    /// <summary>Logs a successful promotion creation.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Information, Message = "Created promotion request {requestId}")]
    public static partial IGenericMessage PromotionCreated(ILogger logger, Guid requestId);

    /// <summary>Logs a failure to create a promotion request.</summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error, Message = "Failed to create promotion request: {message}")]
    public static partial IGenericMessage CreatePromotionFailed(ILogger logger, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Approve/Reject Operations (7320-7329)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs the start of approving a promotion request.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Trace, Message = "Approving promotion request {requestId} by '{approvedBy}'")]
    public static partial IGenericMessage ApprovingPromotion(ILogger logger, Guid requestId, string approvedBy);

    /// <summary>Logs a successful promotion approval.</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Information, Message = "Approved promotion request {requestId}")]
    public static partial IGenericMessage PromotionApproved(ILogger logger, Guid requestId);

    /// <summary>Logs a failure to approve a promotion request.</summary>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error, Message = "Failed to approve promotion request {requestId}: {message}")]
    public static partial IGenericMessage ApprovePromotionFailed(ILogger logger, Guid requestId, string message);

    /// <summary>Logs the start of rejecting a promotion request.</summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Trace, Message = "Rejecting promotion request {requestId} by '{rejectedBy}'")]
    public static partial IGenericMessage RejectingPromotion(ILogger logger, Guid requestId, string rejectedBy);

    /// <summary>Logs a successful promotion rejection.</summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Information, Message = "Rejected promotion request {requestId}")]
    public static partial IGenericMessage PromotionRejected(ILogger logger, Guid requestId);

    /// <summary>Logs a failure to reject a promotion request.</summary>
    [MessageLogging(EventId = 91005, Level = LogLevel.Error, Message = "Failed to reject promotion request {requestId}: {message}")]
    public static partial IGenericMessage RejectPromotionFailed(ILogger logger, Guid requestId, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Execute Operations (7330-7334)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs the start of executing a promotion request.</summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Trace, Message = "Executing promotion request {requestId}")]
    public static partial IGenericMessage ExecutingPromotion(ILogger logger, Guid requestId);

    /// <summary>Logs a successful promotion execution.</summary>
    [MessageLogging(EventId = 11012, Level = LogLevel.Information, Message = "Executed promotion request {requestId}: {successfulItems}/{totalItems} successful")]
    public static partial IGenericMessage PromotionExecuted(ILogger logger, Guid requestId, int successfulItems, int totalItems);

    /// <summary>Logs a failure to execute a promotion request.</summary>
    [MessageLogging(EventId = 91006, Level = LogLevel.Error, Message = "Failed to execute promotion request {requestId}: {message}")]
    public static partial IGenericMessage ExecutePromotionFailed(ILogger logger, Guid requestId, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Compare Operations (7335-7339)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs the start of comparing environments.</summary>
    [MessageLogging(EventId = 11013, Level = LogLevel.Trace, Message = "Comparing environments '{sourceEnvironment}' vs '{targetEnvironment}' for {entityType} '{entityName}'")]
    public static partial IGenericMessage ComparingEnvironments(ILogger logger, string sourceEnvironment, string targetEnvironment, string entityType, string entityName);

    /// <summary>Logs a successful environment comparison.</summary>
    [MessageLogging(EventId = 11014, Level = LogLevel.Information, Message = "Found {count} differences between '{sourceEnvironment}' and '{targetEnvironment}'")]
    public static partial IGenericMessage EnvironmentsCompared(ILogger logger, int count, string sourceEnvironment, string targetEnvironment);

    /// <summary>Logs a failure to compare environments.</summary>
    [MessageLogging(EventId = 91007, Level = LogLevel.Error, Message = "Failed to compare environments '{sourceEnvironment}' vs '{targetEnvironment}': {message}")]
    public static partial IGenericMessage CompareEnvironmentsFailed(ILogger logger, string sourceEnvironment, string targetEnvironment, string message);
}
