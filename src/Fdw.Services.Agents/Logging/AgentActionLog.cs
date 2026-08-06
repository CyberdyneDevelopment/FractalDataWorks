using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Agents.Logging;

/// <summary>
/// MessageLogging definitions for Agent Action service operations.
/// EventId range: 4210-4229
/// </summary>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("AGENTS")]
public static partial class AgentActionLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // List Events (4210-4212)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs the start of listing agent actions.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Information,
        Message = "Listing agent actions with status filter '{status}'")]
    public static partial IGenericMessage ListingAgentActions(ILogger logger, string status);

    /// <summary>Logs a successful list of agent actions.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "Found {count} agent actions")]
    public static partial IGenericMessage AgentActionsListed(ILogger logger, int count);

    /// <summary>Logs a failure to list agent actions.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "Failed to list agent actions: {message}")]
    public static partial IGenericMessage ListFailed(ILogger logger, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Get Events (4213-4216)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs the start of fetching a single agent action.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information,
        Message = "Fetching agent action {actionId}")]
    public static partial IGenericMessage FetchingAgentAction(ILogger logger, int actionId);

    /// <summary>Logs a successful retrieval of an agent action.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Information,
        Message = "Agent action {actionId} retrieved")]
    public static partial IGenericMessage AgentActionRetrieved(ILogger logger, int actionId);

    /// <summary>Logs a failure to fetch an agent action.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "Failed to fetch agent action {actionId}: {message}")]
    public static partial IGenericMessage FetchFailed(ILogger logger, int actionId, string message);

    /// <summary>Logs that an agent action was not found.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning,
        Message = "Agent action {actionId} not found")]
    public static partial IGenericMessage AgentActionNotFound(ILogger logger, int actionId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Review Events (4217-4221)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs the start of a review operation.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information,
        Message = "Reviewing agent action {actionId} with status '{newStatus}'")]
    public static partial IGenericMessage ReviewingAgentAction(ILogger logger, int actionId, string newStatus);

    /// <summary>Logs a successful review of an agent action.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Information,
        Message = "Agent action {actionId} reviewed as '{status}' by '{reviewedBy}'")]
    public static partial IGenericMessage AgentActionReviewed(ILogger logger, int actionId, string status, string reviewedBy);

    /// <summary>Logs a failure to review an agent action.</summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error,
        Message = "Failed to review agent action {actionId} as '{status}': {message}")]
    public static partial IGenericMessage ReviewFailed(ILogger logger, int actionId, string status, string message);

    /// <summary>Logs that an agent action was already reviewed.</summary>
    [MessageLogging(EventId = 41000, Level = LogLevel.Warning,
        Message = "Agent action {actionId} already reviewed with status '{currentStatus}'")]
    public static partial IGenericMessage ActionAlreadyReviewed(ILogger logger, int actionId, string currentStatus);

    /// <summary>Logs that the user identity claim was not found on the request.</summary>
    [MessageLogging(EventId = 51000, Level = LogLevel.Warning,
        Message = "User identity claim not found for agent action request")]
    public static partial IGenericMessage UserClaimNotFound(ILogger logger);

    /// <summary>Logs that an agent action could not be found during a review operation.</summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "Agent action {actionId} not found during review")]
    public static partial IGenericMessage ReviewTargetNotFound(ILogger logger, int actionId);
}
