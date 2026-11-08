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
    public static partial IGenericMessage GettingAgentAction(ILogger logger, Guid actionId);

    /// <summary>Logs that an agent action was not found.</summary>
    [MessageLogging(EventId = 31003, Level = LogLevel.Warning, Message = "Agent action {actionId} not found", ClientMessage = "Agent action {actionId} not found")]
    public static partial IGenericMessage AgentActionNotFound(ILogger logger, Guid actionId);

    /// <summary>Logs the start of a review operation on an agent action.</summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Trace, Message = "Reviewing agent action {actionId} as '{decision}'")]
    public static partial IGenericMessage ReviewingAgentAction(ILogger logger, Guid actionId, string decision);

    /// <summary>Logs a successful review of an agent action.</summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Information, Message = "Agent action {actionId} reviewed as '{decision}' by '{reviewedBy}'")]
    public static partial IGenericMessage AgentActionReviewed(ILogger logger, Guid actionId, string decision, string reviewedBy);

    /// <summary>Logs a failure to review an agent action.</summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error, Message = "Failed to review agent action {actionId} as '{decision}': {message}")]
    public static partial IGenericMessage ReviewAgentActionFailed(ILogger logger, Guid actionId, string decision, string message);

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
    [MessageLogging(EventId = 31004, Level = LogLevel.Warning, Message = "Service '{serviceName}' not found", ClientMessage = "Service '{serviceName}' not found")]
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
    [MessageLogging(EventId = 31005, Level = LogLevel.Warning, Message = "Escalation policy {policyId} not found", ClientMessage = "Escalation policy {policyId} not found")]
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

    /// <summary>Logs that the loaded TypeCollections were listed.</summary>
    [MessageLogging(EventId = 11036, Level = LogLevel.Trace, Message = "Listing {count} loaded TypeCollections")]
    public static partial IGenericMessage TypeCollectionsListed(ILogger logger, int count);

    /// <summary>Logs that the request failed: Failed to list escalation policies.</summary>
    [MessageLogging(EventId = 91014, Level = LogLevel.Error, Message = "Failed to list escalation policies", ClientMessage = "Failed to list escalation policies")]
    public static partial IGenericMessage ListEscalationPoliciesFaulted(ILogger logger);

    /// <summary>Logs that the request failed: Failed to deny agent action.</summary>
    [MessageLogging(EventId = 91015, Level = LogLevel.Error, Message = "Failed to deny agent action", ClientMessage = "Failed to deny agent action")]
    public static partial IGenericMessage DenyAgentActionFaulted(ILogger logger);

    /// <summary>Logs that the request failed: Failed to retrieve service health.</summary>
    [MessageLogging(EventId = 91016, Level = LogLevel.Error, Message = "Failed to retrieve service health", ClientMessage = "Failed to retrieve service health")]
    public static partial IGenericMessage GetServiceHealthFaulted(ILogger logger);

    /// <summary>Logs that the request failed: Failed to retrieve system health.</summary>
    [MessageLogging(EventId = 91017, Level = LogLevel.Error, Message = "Failed to retrieve system health", ClientMessage = "Failed to retrieve system health")]
    public static partial IGenericMessage GetSystemHealthReturnedNothing(ILogger logger);

    /// <summary>Logs that the request failed: Failed to retrieve system health.</summary>
    [MessageLogging(EventId = 91018, Level = LogLevel.Error, Message = "Failed to retrieve system health", ClientMessage = "Failed to retrieve system health")]
    public static partial IGenericMessage GetSystemHealthFaulted(ILogger logger);

    /// <summary>Logs that the request failed: Invalid window format. Use format like '1h', '24h', '7d'.</summary>
    [MessageLogging(EventId = 21002, Level = LogLevel.Warning, Message = "Invalid window format. Use format like '1h', '24h', '7d'.", ClientMessage = "Invalid window format. Use format like '1h', '24h', '7d'.")]
    public static partial IGenericMessage GetServiceHealthHistoryInvalidWindowFormatUseFormat(ILogger logger);

    /// <summary>Logs that the request failed: Failed to retrieve health history.</summary>
    [MessageLogging(EventId = 91019, Level = LogLevel.Error, Message = "Failed to retrieve health history", ClientMessage = "Failed to retrieve health history")]
    public static partial IGenericMessage GetServiceHealthHistoryReturnedNothing(ILogger logger);

    /// <summary>Logs that the request failed: Failed to retrieve health history.</summary>
    [MessageLogging(EventId = 91020, Level = LogLevel.Error, Message = "Failed to retrieve health history", ClientMessage = "Failed to retrieve health history")]
    public static partial IGenericMessage GetServiceHealthHistoryFaulted(ILogger logger);

    /// <summary>Logs that the request failed: Failed to get escalation policy.</summary>
    [MessageLogging(EventId = 91021, Level = LogLevel.Error, Message = "Failed to get escalation policy", ClientMessage = "Failed to get escalation policy")]
    public static partial IGenericMessage GetEscalationPolicyFaulted(ILogger logger);

    /// <summary>Logs that the request failed: Failed to create escalation policy.</summary>
    [MessageLogging(EventId = 91022, Level = LogLevel.Error, Message = "Failed to create escalation policy", ClientMessage = "Failed to create escalation policy")]
    public static partial IGenericMessage CreateEscalationPolicyReturnedNothing(ILogger logger);

    /// <summary>Logs that the request failed: Failed to create escalation policy.</summary>
    [MessageLogging(EventId = 91023, Level = LogLevel.Error, Message = "Failed to create escalation policy", ClientMessage = "Failed to create escalation policy")]
    public static partial IGenericMessage CreateEscalationPolicyFaulted(ILogger logger);

    /// <summary>Logs that the request failed: Invalid window format. Use format like '5m', '1h', '24h'.</summary>
    [MessageLogging(EventId = 21003, Level = LogLevel.Warning, Message = "Invalid window format. Use format like '5m', '1h', '24h'.", ClientMessage = "Invalid window format. Use format like '5m', '1h', '24h'.")]
    public static partial IGenericMessage GetServiceThroughputInvalidWindowFormatUseFormat(ILogger logger);

    /// <summary>Logs that the request failed: Failed to retrieve throughput data.</summary>
    [MessageLogging(EventId = 91024, Level = LogLevel.Error, Message = "Failed to retrieve throughput data", ClientMessage = "Failed to retrieve throughput data")]
    public static partial IGenericMessage GetServiceThroughputReturnedNothing(ILogger logger);

    /// <summary>Logs that the request failed: Failed to retrieve throughput data.</summary>
    [MessageLogging(EventId = 91025, Level = LogLevel.Error, Message = "Failed to retrieve throughput data", ClientMessage = "Failed to retrieve throughput data")]
    public static partial IGenericMessage GetServiceThroughputFaulted(ILogger logger);

    /// <summary>Logs that the request failed: Failed to approve agent action.</summary>
    [MessageLogging(EventId = 91026, Level = LogLevel.Error, Message = "Failed to approve agent action", ClientMessage = "Failed to approve agent action")]
    public static partial IGenericMessage ApproveAgentActionFaulted(ILogger logger);

    /// <summary>Logs that the request failed: Failed to delete escalation policy.</summary>
    [MessageLogging(EventId = 91027, Level = LogLevel.Error, Message = "Failed to delete escalation policy", ClientMessage = "Failed to delete escalation policy")]
    public static partial IGenericMessage DeleteEscalationPolicyFaulted(ILogger logger);

    /// <summary>Logs that the request failed: Failed to get agent action.</summary>
    [MessageLogging(EventId = 91028, Level = LogLevel.Error, Message = "Failed to get agent action", ClientMessage = "Failed to get agent action")]
    public static partial IGenericMessage GetAgentActionFaulted(ILogger logger);

    /// <summary>Logs that the request failed: Failed to list agent actions.</summary>
    [MessageLogging(EventId = 91029, Level = LogLevel.Error, Message = "Failed to list agent actions", ClientMessage = "Failed to list agent actions")]
    public static partial IGenericMessage ListAgentActionsFaulted(ILogger logger);

    /// <summary>Logs that the request failed: Failed to update escalation policy.</summary>
    [MessageLogging(EventId = 31006, Level = LogLevel.Warning, Message = "Escalation policy to update was not found", ClientMessage = "The escalation policy to update was not found")]
    public static partial IGenericMessage UpdateEscalationPolicyNotFound(ILogger logger);

    /// <summary>Logs that the request failed: Failed to update escalation policy.</summary>
    [MessageLogging(EventId = 91030, Level = LogLevel.Error, Message = "Failed to update escalation policy", ClientMessage = "Failed to update escalation policy")]
    public static partial IGenericMessage UpdateEscalationPolicyReturnedNothing(ILogger logger);

    /// <summary>Logs that the request failed: Failed to update escalation policy.</summary>
    [MessageLogging(EventId = 91031, Level = LogLevel.Error, Message = "Failed to update escalation policy", ClientMessage = "Failed to update escalation policy")]
    public static partial IGenericMessage UpdateEscalationPolicyFaulted(ILogger logger);

    /// <summary>Logs that Data set '{dataSetName}' was not found for its lineage.</summary>
    [MessageLogging(EventId = 31007, Level = LogLevel.Warning, Message = "Data set '{dataSetName}' was not found for its lineage", ClientMessage = "Data set '{dataSetName}' was not found")]
    public static partial IGenericMessage DataSetLineageDataSetNotFound(ILogger logger, string dataSetName);

    /// <summary>Logs that Execution {executionId} was not found, so it has no children.</summary>
    [MessageLogging(EventId = 31008, Level = LogLevel.Warning, Message = "Execution {executionId} was not found, so it has no children", ClientMessage = "Execution {executionId} was not found")]
    public static partial IGenericMessage ExecutionChildrenExecutionNotFound(ILogger logger, Guid executionId);

    /// <summary>Logs that Execution {executionId} was not found, so it has no events.</summary>
    [MessageLogging(EventId = 31009, Level = LogLevel.Warning, Message = "Execution {executionId} was not found, so it has no events", ClientMessage = "Execution {executionId} was not found")]
    public static partial IGenericMessage ExecutionEventsExecutionNotFound(ILogger logger, Guid executionId);

    /// <summary>Logs that Execution {executionId} was not found.</summary>
    [MessageLogging(EventId = 31010, Level = LogLevel.Warning, Message = "Execution {executionId} was not found", ClientMessage = "Execution {executionId} was not found")]
    public static partial IGenericMessage GetExecutionExecutionNotFound(ILogger logger, Guid executionId);

    /// <summary>Logs that Pipeline '{pipelineName}' was not found for the dataflow graph.</summary>
    [MessageLogging(EventId = 31011, Level = LogLevel.Warning, Message = "Pipeline '{pipelineName}' was not found for the dataflow graph", ClientMessage = "Pipeline '{pipelineName}' was not found")]
    public static partial IGenericMessage DataflowPipelineNotFound(ILogger logger, string pipelineName);

    /// <summary>Logs that Execution {executionId} was not found, so it cannot be decided.</summary>
    [MessageLogging(EventId = 31012, Level = LogLevel.Warning, Message = "Execution {executionId} was not found, so it cannot be decided", ClientMessage = "Execution {executionId} was not found")]
    public static partial IGenericMessage DecideExecutionExecutionNotFound(ILogger logger, Guid executionId);

    /// <summary>Logs that Execution {executionId} is '{state}', not AwaitingApproval, so it cannot be decided.</summary>
    [MessageLogging(EventId = 40001, Level = LogLevel.Warning, Message = "Execution {executionId} is '{state}', not AwaitingApproval, so it cannot be decided", ClientMessage = "The execution is '{state}', not AwaitingApproval, so it cannot be decided")]
    public static partial IGenericMessage DecideExecutionNotAwaitingApproval(ILogger logger, Guid executionId, string state);

    /// <summary>Logs that Execution {executionId} was decided by someone else first.</summary>
    [MessageLogging(EventId = 40002, Level = LogLevel.Warning, Message = "Execution {executionId} was decided by someone else first", ClientMessage = "The execution was decided by someone else first")]
    public static partial IGenericMessage DecideExecutionDecidedByAnother(ILogger logger, Guid executionId);

    /// <summary>Logs that An escalation policy was refused: required and not stated: {fields}.</summary>
    [MessageLogging(EventId = 21004, Level = LogLevel.Warning, Message = "An escalation policy was refused: required and not stated: {fields}", ClientMessage = "Required and not stated: {fields}")]
    public static partial IGenericMessage CreateEscalationPolicyFieldsNotStated(ILogger logger, string fields);

    /// <summary>Logs that An update of an escalation policy was refused: required and not stated: {fields}.</summary>
    [MessageLogging(EventId = 21005, Level = LogLevel.Warning, Message = "An update of an escalation policy was refused: required and not stated: {fields}", ClientMessage = "Required and not stated: {fields}")]
    public static partial IGenericMessage UpdateEscalationPolicyFieldsNotStated(ILogger logger, string fields);

    /// <summary>Logs that A list of escalation alerts was refused: State and Open were both stated.</summary>
    [MessageLogging(EventId = 21006, Level = LogLevel.Warning, Message = "A list of escalation alerts was refused: State and Open were both stated", ClientMessage = "State and Open cannot both be stated")]
    public static partial IGenericMessage ListEscalationAlertsStateAndOpenTogether(ILogger logger);

    /// <summary>Logs that The alerts service answered a list with a success and no value.</summary>
    [MessageLogging(EventId = 91032, Level = LogLevel.Error, Message = "The alerts service answered a list with a success and no value", ClientMessage = "The escalation alerts could not be listed")]
    public static partial IGenericMessage ListEscalationAlertsReturnedNothing(ILogger logger);

    /// <summary>Logs that The alerts service resolved alert {alertId} with a success and no value.</summary>
    [MessageLogging(EventId = 91033, Level = LogLevel.Error, Message = "The alerts service resolved alert {alertId} with a success and no value", ClientMessage = "The escalation alert could not be resolved")]
    public static partial IGenericMessage ResolveEscalationAlertReturnedNothing(ILogger logger, Guid alertId);

    /// <summary>Logs that The alerts service acknowledged alert {alertId} with a success and no value.</summary>
    [MessageLogging(EventId = 91034, Level = LogLevel.Error, Message = "The alerts service acknowledged alert {alertId} with a success and no value", ClientMessage = "The escalation alert could not be acknowledged")]
    public static partial IGenericMessage AcknowledgeEscalationAlertReturnedNothing(ILogger logger, Guid alertId);

    /// <summary>Logs that the paging of a list of escalation alerts answered a success and no value.</summary>
    [MessageLogging(EventId = 91035, Level = LogLevel.Error, Message = "The paging of a list of escalation alerts answered a success and no value", ClientMessage = "The escalation alerts could not be listed")]
    public static partial IGenericMessage ListEscalationAlertsPagingReturnedNothing(ILogger logger);

    /// <summary>Logs that A list of executions named the item type '{itemType}', which is not one.</summary>
    [MessageLogging(EventId = 21007, Level = LogLevel.Warning, Message = "A list of executions named the item type '{itemType}', which is not one; the types are {itemTypes}", ClientMessage = "'{itemType}' is not an execution item type; the types are {itemTypes}")]
    public static partial IGenericMessage ListExecutionsItemTypeUnknown(ILogger logger, string itemType, string itemTypes);

    /// <summary>Logs that The execution tracker answered a list with a success and no value.</summary>
    [MessageLogging(EventId = 91036, Level = LogLevel.Error, Message = "The execution tracker answered a list with a success and no value", ClientMessage = "The executions could not be listed")]
    public static partial IGenericMessage ListExecutionsReturnedNothing(ILogger logger);

    /// <summary>Logs that The request to deny agent action was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 51001, Level = LogLevel.Warning, Message = "The request to deny agent action was refused: the caller is not signed in", ClientMessage = "You must be signed in to deny agent action")]
    public static partial IGenericMessage DenyAgentActionNotSignedIn(ILogger logger);

    /// <summary>Logs that The request to approve agent action was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 51002, Level = LogLevel.Warning, Message = "The request to approve agent action was refused: the caller is not signed in", ClientMessage = "You must be signed in to approve agent action")]
    public static partial IGenericMessage ApproveAgentActionNotSignedIn(ILogger logger);

    /// <summary>Logs that The request to decide execution was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 51003, Level = LogLevel.Warning, Message = "The request to decide execution was refused: the caller is not signed in", ClientMessage = "You must be signed in to decide execution")]
    public static partial IGenericMessage DecideExecutionNotSignedIn(ILogger logger);

    /// <summary>Logs that The request to acknowledge escalation alert was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 51004, Level = LogLevel.Warning, Message = "The request to acknowledge escalation alert was refused: the caller is not signed in", ClientMessage = "You must be signed in to acknowledge escalation alert")]
    public static partial IGenericMessage AcknowledgeEscalationAlertNotSignedIn(ILogger logger);

    /// <summary>Logs that The request to resolve escalation alert was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 51005, Level = LogLevel.Warning, Message = "The request to resolve escalation alert was refused: the caller is not signed in", ClientMessage = "You must be signed in to resolve escalation alert")]
    public static partial IGenericMessage ResolveEscalationAlertNotSignedIn(ILogger logger);
}
