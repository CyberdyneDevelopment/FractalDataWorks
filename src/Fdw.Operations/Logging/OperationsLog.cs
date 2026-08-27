using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Operations.Logging;

/// <summary>
/// MessageLogging for Operations execution tracking.
/// EventId range: 7700-7799
/// </summary>
[MessageLoggingTypeCode("OPERATIONS")]
public static partial class OperationsLog
{
    // =============================================================================
    // Execution Item Events (7700-7719)
    // =============================================================================

    /// <summary>
    /// Logs when an execution item is not found.
    /// </summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning, Message = "Execution item '{executionItemId}' not found")]
    public static partial IGenericMessage ExecutionItemNotFound(ILogger logger, Guid executionItemId);

    /// <summary>
    /// Logs when an execution item is created.
    /// </summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Information, Message = "Created execution item '{name}' ({itemType}) with ID {executionItemId}")]
    public static partial IGenericMessage ExecutionItemCreated(ILogger logger, Guid executionItemId, string name, string itemType);

    /// <summary>
    /// Logs when an invalid state transition is attempted.
    /// </summary>
    [MessageLogging(EventId = 41000, Level = LogLevel.Warning, Message = "Invalid state transition from '{currentState}' to '{newState}' for item '{executionItemId}'")]
    public static partial IGenericMessage InvalidStateTransition(ILogger logger, Guid executionItemId, string currentState, string newState);

    /// <summary>
    /// Logs when a state transition is recorded.
    /// </summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Debug, Message = "State transition recorded: {executionItemId} {previousState} -> {newState}")]
    public static partial IGenericMessage StateTransitionRecorded(ILogger logger, Guid executionItemId, string previousState, string newState);

    /// <summary>
    /// Logs when attempting to modify an already completed item.
    /// </summary>
    [MessageLogging(EventId = 41001, Level = LogLevel.Warning, Message = "Execution item '{executionItemId}' is already in terminal state '{state}'")]
    public static partial IGenericMessage ExecutionItemAlreadyCompleted(ILogger logger, Guid executionItemId, string state);

    /// <summary>
    /// Logs when execution item name is required but not provided.
    /// </summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error, Message = "Execution item name is required")]
    public static partial IGenericMessage ExecutionItemNameRequired(ILogger logger);

    /// <summary>
    /// Logs when persisting an execution item fails.
    /// </summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "Failed to persist execution item '{executionItemId}': {errorMessage}")]
    public static partial IGenericMessage ExecutionItemPersistFailed(ILogger logger, Guid executionItemId, string errorMessage);

    /// <summary>
    /// Logs when an execution item is completed successfully.
    /// </summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information, Message = "Execution item '{executionItemId}' completed successfully")]
    public static partial IGenericMessage ExecutionItemCompletedSuccess(ILogger logger, Guid executionItemId);

    /// <summary>
    /// Logs when an execution item fails.
    /// </summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Execution item '{executionItemId}' failed: {resultCode}")]
    public static partial IGenericMessage ExecutionItemCompletedFailure(ILogger logger, Guid executionItemId, string? resultCode);

    /// <summary>
    /// Logs when an event is recorded for an execution item.
    /// </summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug, Message = "Event '{eventType}' recorded for execution item '{executionItemId}'")]
    public static partial IGenericMessage EventRecorded(ILogger logger, Guid executionItemId, string eventType);

    /// <summary>
    /// Logs when parent execution item validation fails.
    /// </summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error, Message = "Parent execution item '{domainConfigurationId}' not found for child '{childName}'")]
    public static partial IGenericMessage ParentExecutionItemNotFound(ILogger logger, Guid domainConfigurationId, string childName);

    /// <summary>
    /// Logs when containment validation fails.
    /// </summary>
    [MessageLogging(EventId = 41002, Level = LogLevel.Error, Message = "'{parentType}' cannot contain '{childType}' - invalid hierarchy")]
    public static partial IGenericMessage InvalidContainment(ILogger logger, string parentType, string childType);

    // =============================================================================
    // Escalation Events (7720-7739)
    // =============================================================================

    /// <summary>
    /// Logs when an escalation is triggered.
    /// </summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Warning, Message = "Escalation triggered for item '{executionItemId}' at level {level}")]
    public static partial IGenericMessage EscalationTriggered(ILogger logger, Guid executionItemId, int level);

    /// <summary>
    /// Logs when escalation is on cooldown.
    /// </summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information, Message = "Escalation cooldown active for item '{executionItemId}', next escalation at {nextEscalationTime}")]
    public static partial IGenericMessage EscalationCooldownActive(ILogger logger, Guid executionItemId, DateTimeOffset nextEscalationTime);

    /// <summary>
    /// Logs when an escalation policy is not found.
    /// </summary>
    [MessageLogging(EventId = 31002, Level = LogLevel.Warning, Message = "Escalation policy '{policyId}' not found")]
    public static partial IGenericMessage EscalationPolicyNotFound(ILogger logger, Guid policyId);

    /// <summary>
    /// Logs when an escalation level is not defined.
    /// </summary>
    [MessageLogging(EventId = 31003, Level = LogLevel.Error, Message = "Escalation level {level} not defined for policy '{policyId}'")]
    public static partial IGenericMessage EscalationLevelNotDefined(ILogger logger, Guid policyId, int level);

    /// <summary>
    /// Logs when escalation is suppressed.
    /// </summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Information, Message = "Escalation suppressed for item '{executionItemId}' due to override")]
    public static partial IGenericMessage EscalationSuppressed(ILogger logger, Guid executionItemId);

    /// <summary>
    /// Logs when an escalation notification fails.
    /// </summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error, Message = "Escalation notification failed for item '{executionItemId}' level {level}: {errorMessage}")]
    public static partial IGenericMessage EscalationNotificationFailed(ILogger logger, Guid executionItemId, int level, string errorMessage);

    // =============================================================================
    // Escalation CRUD Events (7726-7739)
    // =============================================================================

    /// <summary>
    /// Logs when fetching an escalation policy.
    /// </summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Debug, Message = "Fetching escalation policy '{policyId}'")]
    public static partial IGenericMessage EscalationFetchingPolicy(ILogger logger, Guid policyId);

    /// <summary>
    /// Logs when listing escalation policies.
    /// </summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Debug, Message = "Listing all escalation policies")]
    public static partial IGenericMessage EscalationListingPolicies(ILogger logger);

    /// <summary>
    /// Logs the count of policies found.
    /// </summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Debug, Message = "Found {count} escalation policies")]
    public static partial IGenericMessage EscalationPoliciesFound(ILogger logger, int count);

    /// <summary>
    /// Logs when creating an escalation policy.
    /// </summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Information, Message = "Creating escalation policy '{name}'")]
    public static partial IGenericMessage EscalationCreatingPolicy(ILogger logger, string name);

    /// <summary>
    /// Logs when an escalation policy is created.
    /// </summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Information, Message = "Created escalation policy '{policyId}' ('{name}')")]
    public static partial IGenericMessage EscalationPolicyCreated(ILogger logger, Guid policyId, string name);

    /// <summary>
    /// Logs when updating an escalation policy.
    /// </summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Information, Message = "Updating escalation policy '{policyId}'")]
    public static partial IGenericMessage EscalationUpdatingPolicy(ILogger logger, Guid policyId);

    /// <summary>
    /// Logs when an escalation policy is updated.
    /// </summary>
    [MessageLogging(EventId = 11012, Level = LogLevel.Information, Message = "Updated escalation policy '{policyId}'")]
    public static partial IGenericMessage EscalationPolicyUpdated(ILogger logger, Guid policyId);

    /// <summary>
    /// Logs when deleting an escalation policy.
    /// </summary>
    [MessageLogging(EventId = 11013, Level = LogLevel.Information, Message = "Deleting escalation policy '{policyId}'")]
    public static partial IGenericMessage EscalationDeletingPolicy(ILogger logger, Guid policyId);

    /// <summary>
    /// Logs when an escalation policy is deleted.
    /// </summary>
    [MessageLogging(EventId = 11014, Level = LogLevel.Information, Message = "Deleted escalation policy '{policyId}'")]
    public static partial IGenericMessage EscalationPolicyDeleted(ILogger logger, Guid policyId);

    /// <summary>
    /// Logs when escalation policy name is required but not provided.
    /// </summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Error, Message = "Escalation policy name is required")]
    public static partial IGenericMessage EscalationPolicyNameRequired(ILogger logger);

    /// <summary>
    /// Logs when persisting an escalation policy fails.
    /// </summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error, Message = "Failed to persist escalation data: {errorMessage}")]
    public static partial IGenericMessage EscalationPersistFailed(ILogger logger, string errorMessage);

    // =============================================================================
    // Trigger Events (7740-7759)
    // =============================================================================

    /// <summary>
    /// Logs when a trigger is accepted.
    /// </summary>
    [MessageLogging(EventId = 11015, Level = LogLevel.Information, Message = "Trigger accepted for workflow '{workflowName}' with execution ID {executionItemId}")]
    public static partial IGenericMessage TriggerAccepted(ILogger logger, string workflowName, Guid executionItemId);

    /// <summary>
    /// Logs when a trigger is rejected due to concurrent execution.
    /// </summary>
    [MessageLogging(EventId = 41003, Level = LogLevel.Warning, Message = "Trigger rejected for workflow '{workflowName}': concurrent execution already running")]
    public static partial IGenericMessage TriggerRejectedConcurrent(ILogger logger, string workflowName);

    /// <summary>
    /// Logs when trigger validation fails.
    /// </summary>
    [MessageLogging(EventId = 21002, Level = LogLevel.Error, Message = "Trigger validation failed for workflow '{workflowName}': {validationError}")]
    public static partial IGenericMessage TriggerValidationFailed(ILogger logger, string workflowName, string validationError);

    /// <summary>
    /// Logs when a trigger is throttled.
    /// </summary>
    [MessageLogging(EventId = 81000, Level = LogLevel.Warning, Message = "Trigger throttled for workflow '{workflowName}', retry after {retryAfter}")]
    public static partial IGenericMessage TriggerThrottled(ILogger logger, string workflowName, DateTimeOffset retryAfter);

    // =============================================================================
    // Correlation Events (7760-7769)
    // =============================================================================

    /// <summary>
    /// Logs when correlation ID lookup finds no results.
    /// </summary>
    [MessageLogging(EventId = 11016, Level = LogLevel.Debug, Message = "No execution items found for correlation ID '{correlationId}'")]
    public static partial IGenericMessage CorrelationIdNotFound(ILogger logger, string correlationId);

    /// <summary>
    /// Logs when correlation ID lookup succeeds.
    /// </summary>
    [MessageLogging(EventId = 11017, Level = LogLevel.Debug, Message = "Found {count} execution items for correlation ID '{correlationId}'")]
    public static partial IGenericMessage CorrelationIdFound(ILogger logger, string correlationId, int count);

    // =============================================================================
    // Notification Emission Events (7770-7776)
    // =============================================================================

    /// <summary>
    /// Logs when notification emission is skipped because providers are not configured.
    /// </summary>
    [MessageLogging(EventId = 11018, Level = LogLevel.Debug, Message = "Notification providers not configured for item '{executionItemId}'; skipping emission")]
    public static partial IGenericMessage NotificationsSkippedNotConfigured(ILogger logger, Guid executionItemId);

    /// <summary>
    /// Logs when loading notification rules fails.
    /// </summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Warning, Message = "Failed to load notification rules for item '{executionItemId}'; skipping emission")]
    public static partial IGenericMessage NotificationRulesLoadFailed(ILogger logger, Guid executionItemId);

    /// <summary>
    /// Logs when a rule references a notification channel that cannot be resolved.
    /// </summary>
    [MessageLogging(EventId = 31004, Level = LogLevel.Warning, Message = "Notification rule '{ruleName}' references unresolved channel '{channelName}'; skipping rule")]
    public static partial IGenericMessage NotificationChannelUnresolved(ILogger logger, string ruleName, string channelName);

    /// <summary>
    /// Logs when a notification is sent successfully.
    /// </summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Information, Message = "Notification sent for item '{executionItemId}' via rule '{ruleName}' on channel '{channelName}'")]
    public static partial IGenericMessage NotificationSent(ILogger logger, Guid executionItemId, string ruleName, string channelName);

    /// <summary>
    /// Logs when sending a notification fails.
    /// </summary>
    [MessageLogging(EventId = 71004, Level = LogLevel.Warning, Message = "Notification send failed for item '{executionItemId}' via rule '{ruleName}' on channel '{channelName}'")]
    public static partial IGenericMessage NotificationSendFailed(ILogger logger, Guid executionItemId, string ruleName, string channelName);

    /// <summary>
    /// Logs when a rule has an invalid severity that does not map to a known priority.
    /// </summary>
    [MessageLogging(EventId = 21003, Level = LogLevel.Warning, Message = "Notification rule '{ruleName}' has invalid severity '{severity}'; skipping rule")]
    public static partial IGenericMessage NotificationSeverityInvalid(ILogger logger, string ruleName, string severity);

    /// <summary>
    /// Logs when an unexpected exception occurs during notification emission.
    /// </summary>
    [MessageLogging(EventId = 91002, Level = LogLevel.Warning, Message = "Unexpected error during notification emission for item '{executionItemId}'; execution result is unaffected")]
    public static partial IGenericMessage NotificationEmissionError(ILogger logger, Guid executionItemId, Exception exception);
}
