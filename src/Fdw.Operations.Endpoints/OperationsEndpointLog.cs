using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// MessageLogging definitions for Operations endpoint base classes.
/// EventId range: 7250-7299
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS3")]
public static partial class OperationsEndpointLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Agent Actions (7250-7259)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs the start of listing agent actions.</summary>
    [MessageLogging(EventId = 11016, Level = LogLevel.Trace, Message = "Listing agent actions with status filter '{status}'")]
    public static partial IGenericMessage ListingAgentActions(ILogger logger, string status);

    /// <summary>Logs a successful list of agent actions.</summary>
    [MessageLogging(EventId = 11017, Level = LogLevel.Information, Message = "Listed {count} agent actions")]
    public static partial IGenericMessage AgentActionsListed(ILogger logger, int count);

    /// <summary>Logs a failure to list agent actions.</summary>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error, Message = "Failed to list agent actions: {message}")]
    public static partial IGenericMessage ListAgentActionsFailed(ILogger logger, string message);

    /// <summary>Logs the start of fetching a single agent action.</summary>
    [MessageLogging(EventId = 11018, Level = LogLevel.Trace, Message = "Getting agent action {actionId}")]
    public static partial IGenericMessage GettingAgentAction(ILogger logger, int actionId);

    /// <summary>Logs that an agent action was not found.</summary>
    [MessageLogging(EventId = 31003, Level = LogLevel.Warning, Message = "Agent action {actionId} not found")]
    public static partial IGenericMessage AgentActionNotFound(ILogger logger, int actionId);

    /// <summary>Logs the start of a review operation on an agent action.</summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Trace, Message = "Reviewing agent action {actionId} as '{decision}'")]
    public static partial IGenericMessage ReviewingAgentAction(ILogger logger, int actionId, string decision);

    /// <summary>Logs a successful review of an agent action.</summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Information, Message = "Agent action {actionId} reviewed as '{decision}' by '{reviewedBy}'")]
    public static partial IGenericMessage AgentActionReviewed(ILogger logger, int actionId, string decision, string reviewedBy);

    /// <summary>Logs a failure to review an agent action.</summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error, Message = "Failed to review agent action {actionId} as '{decision}': {message}")]
    public static partial IGenericMessage ReviewAgentActionFailed(ILogger logger, int actionId, string decision, string message);

    /// <summary>Logs that the user identity claim was not found.</summary>
    [MessageLogging(EventId = 51000, Level = LogLevel.Warning, Message = "User identity claim not found for agent action review")]
    public static partial IGenericMessage AgentActionUserClaimNotFound(ILogger logger);

    // ═══════════════════════════════════════════════════════════════════════════
    // Health (7260-7269)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs the start of getting system health.</summary>
    [MessageLogging(EventId = 11021, Level = LogLevel.Trace, Message = "Getting system health snapshot")]
    public static partial IGenericMessage GettingSystemHealth(ILogger logger);

    /// <summary>Logs a failure to get system health.</summary>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error, Message = "Failed to get system health: {message}")]
    public static partial IGenericMessage GetSystemHealthFailed(ILogger logger, string message);

    /// <summary>Logs the start of getting service health.</summary>
    [MessageLogging(EventId = 11022, Level = LogLevel.Trace, Message = "Getting health for service '{serviceName}'")]
    public static partial IGenericMessage GettingServiceHealth(ILogger logger, string serviceName);

    /// <summary>Logs that a service was not found.</summary>
    [MessageLogging(EventId = 31004, Level = LogLevel.Warning, Message = "Service '{serviceName}' not found")]
    public static partial IGenericMessage ServiceNotFound(ILogger logger, string serviceName);

    /// <summary>Logs the start of getting service health history.</summary>
    [MessageLogging(EventId = 11023, Level = LogLevel.Trace, Message = "Getting health history for service '{serviceName}' with window '{window}'")]
    public static partial IGenericMessage GettingServiceHealthHistory(ILogger logger, string serviceName, string window);

    /// <summary>Logs a failure to get health history.</summary>
    [MessageLogging(EventId = 91005, Level = LogLevel.Error, Message = "Failed to get health history for service '{serviceName}': {message}")]
    public static partial IGenericMessage GetHealthHistoryFailed(ILogger logger, string serviceName, string message);

    /// <summary>Logs the start of getting service throughput.</summary>
    [MessageLogging(EventId = 11024, Level = LogLevel.Trace, Message = "Getting throughput for service '{serviceName}' with window '{window}'")]
    public static partial IGenericMessage GettingServiceThroughput(ILogger logger, string serviceName, string window);

    /// <summary>Logs a failure to get throughput.</summary>
    [MessageLogging(EventId = 91006, Level = LogLevel.Error, Message = "Failed to get throughput for service '{serviceName}': {message}")]
    public static partial IGenericMessage GetThroughputFailed(ILogger logger, string serviceName, string message);

    /// <summary>Logs an invalid window format.</summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Warning, Message = "Invalid time window format '{window}'")]
    public static partial IGenericMessage InvalidWindowFormat(ILogger logger, string window);

    // ═══════════════════════════════════════════════════════════════════════════
    // Audit (7270-7279)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs the start of listing audit records.</summary>
    [MessageLogging(EventId = 11025, Level = LogLevel.Trace, Message = "Listing audit records: entityType={entityType}, action={action}")]
    public static partial IGenericMessage ListingAuditRecords(ILogger logger, string? entityType, string? action);

    /// <summary>Logs the number of audit records found.</summary>
    [MessageLogging(EventId = 11026, Level = LogLevel.Information, Message = "Found {count} audit records")]
    public static partial IGenericMessage AuditRecordsFound(ILogger logger, int count);

    /// <summary>Logs a failure to list audit records (with diagnostic message from the result).</summary>
    [MessageLogging(EventId = 91007, Level = LogLevel.Error, Message = "Failed to list audit records: {message}")]
    public static partial IGenericMessage ListAuditRecordsFailed(ILogger logger, string message);

    /// <summary>Logs a failure to list audit records with no additional message context.</summary>
    [MessageLogging(EventId = 91008, Level = LogLevel.Error, Message = "Failed to list audit records")]
    public static partial IGenericMessage ListAuditRecordsFailed(ILogger logger);

    // ═══════════════════════════════════════════════════════════════════════════
    // Escalation (7280-7299)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs the start of listing escalation policies.</summary>
    [MessageLogging(EventId = 11027, Level = LogLevel.Trace, Message = "Listing escalation policies")]
    public static partial IGenericMessage ListingEscalationPolicies(ILogger logger);

    /// <summary>Logs the number of escalation policies found.</summary>
    [MessageLogging(EventId = 11028, Level = LogLevel.Information, Message = "Found {count} escalation policies")]
    public static partial IGenericMessage EscalationPoliciesFound(ILogger logger, int count);

    /// <summary>Logs a failure to list escalation policies.</summary>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error, Message = "Failed to list escalation policies: {message}")]
    public static partial IGenericMessage ListEscalationPoliciesFailed(ILogger logger, string message);

    /// <summary>Logs the start of fetching an escalation policy.</summary>
    [MessageLogging(EventId = 11029, Level = LogLevel.Trace, Message = "Getting escalation policy {policyId}")]
    public static partial IGenericMessage GettingEscalationPolicy(ILogger logger, Guid policyId);

    /// <summary>Logs that an escalation policy was not found.</summary>
    [MessageLogging(EventId = 31005, Level = LogLevel.Warning, Message = "Escalation policy {policyId} not found")]
    public static partial IGenericMessage EscalationPolicyNotFound(ILogger logger, Guid policyId);

    /// <summary>Logs a failure to get an escalation policy.</summary>
    [MessageLogging(EventId = 91010, Level = LogLevel.Error, Message = "Failed to get escalation policy {policyId}: {message}")]
    public static partial IGenericMessage GetEscalationPolicyFailed(ILogger logger, Guid policyId, string message);

    /// <summary>Logs the start of creating an escalation policy.</summary>
    [MessageLogging(EventId = 11030, Level = LogLevel.Trace, Message = "Creating escalation policy '{name}'")]
    public static partial IGenericMessage CreatingEscalationPolicy(ILogger logger, string name);

    /// <summary>Logs a failure to create an escalation policy.</summary>
    [MessageLogging(EventId = 91011, Level = LogLevel.Error, Message = "Failed to create escalation policy: {message}")]
    public static partial IGenericMessage CreateEscalationPolicyFailed(ILogger logger, string message);

    /// <summary>Logs the start of updating an escalation policy.</summary>
    [MessageLogging(EventId = 11031, Level = LogLevel.Trace, Message = "Updating escalation policy {policyId}")]
    public static partial IGenericMessage UpdatingEscalationPolicy(ILogger logger, Guid policyId);

    /// <summary>Logs a failure to update an escalation policy.</summary>
    [MessageLogging(EventId = 91012, Level = LogLevel.Error, Message = "Failed to update escalation policy {policyId}: {message}")]
    public static partial IGenericMessage UpdateEscalationPolicyFailed(ILogger logger, Guid policyId, string message);

    /// <summary>Logs the start of deleting an escalation policy.</summary>
    [MessageLogging(EventId = 11032, Level = LogLevel.Trace, Message = "Deleting escalation policy {policyId}")]
    public static partial IGenericMessage DeletingEscalationPolicy(ILogger logger, Guid policyId);

    /// <summary>Logs a failure to delete an escalation policy.</summary>
    [MessageLogging(EventId = 91013, Level = LogLevel.Error, Message = "Failed to delete escalation policy {policyId}: {message}")]
    public static partial IGenericMessage DeleteEscalationPolicyFailed(ILogger logger, Guid policyId, string message);

    /// <summary>Logs a successful escalation policy creation.</summary>
    [MessageLogging(EventId = 11033, Level = LogLevel.Information, Message = "Created escalation policy '{name}'")]
    public static partial IGenericMessage EscalationPolicyCreated(ILogger logger, string name);

    /// <summary>Logs a successful escalation policy update.</summary>
    [MessageLogging(EventId = 11034, Level = LogLevel.Information, Message = "Updated escalation policy {policyId}")]
    public static partial IGenericMessage EscalationPolicyUpdated(ILogger logger, Guid policyId);

    /// <summary>Logs a successful escalation policy deletion.</summary>
    [MessageLogging(EventId = 11035, Level = LogLevel.Information, Message = "Deleted escalation policy {policyId}")]
    public static partial IGenericMessage EscalationPolicyDeleted(ILogger logger, Guid policyId);
}
