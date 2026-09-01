using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Scheduling.Logging;

/// <summary>
/// MessageLogging methods for Scheduling service operations.
/// Every log message is returned in the result AND logged.
/// EventId range: 8201-8299
/// </summary>
[ExcludeFromCodeCoverage(Justification = "MessageLogging partial class - implementation is source-generated")]
[MessageLoggingTypeCode("SCHEDULING2")]
public static partial class SchedulingLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Provider Events (8201-8210)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when getting a scheduling service by name.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Debug,
        Message = "Getting scheduling service by name '{configurationName}'")]
    public static partial IGenericMessage GettingSchedulingByName(
        ILogger logger,
        string configurationName);

    /// <summary>
    /// Logs when getting a scheduling service by configuration.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "Getting scheduling service for type '{schedulingType}'")]
    public static partial IGenericMessage GettingScheduling(
        ILogger logger,
        string schedulingType);

    /// <summary>
    /// Logs when getting a typed scheduling service.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "Getting typed scheduling service '{serviceType}' by name '{configurationName}'")]
    public static partial IGenericMessage GettingTypedScheduling(
        ILogger logger,
        string serviceType,
        string configurationName);

    /// <summary>
    /// Logs when a scheduling configuration is loaded.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "Scheduling configuration loaded: '{configurationName}' (type: {schedulingType})")]
    public static partial IGenericMessage ConfigurationLoaded(
        ILogger logger,
        string configurationName,
        string schedulingType);

    /// <summary>
    /// Logs when a factory is registered with the provider.
    /// </summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "Scheduling factory registered for type '{schedulingType}'")]
    public static partial IGenericMessage FactoryRegistered(
        ILogger logger,
        string schedulingType);

    /// <summary>
    /// Logs when creating a scheduling service with a factory.
    /// </summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Debug,
        Message = "Creating scheduling service '{configurationName}' with factory '{factoryName}'")]
    public static partial IGenericMessage CreatingWithFactory(
        ILogger logger,
        string configurationName,
        string factoryName);

    /// <summary>
    /// Logs when the configuration cache is cleared.
    /// </summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Information,
        Message = "Scheduling configuration cache cleared ({count} entries removed)")]
    public static partial IGenericMessage CacheCleared(
        ILogger logger,
        int count);

    /// <summary>
    /// Logs when a type cast succeeds.
    /// </summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Debug,
        Message = "Scheduling service cast succeeded to '{serviceType}'")]
    public static partial IGenericMessage CastSucceeded(
        ILogger logger,
        string serviceType);

    // ═══════════════════════════════════════════════════════════════════════════
    // Task Execution Events (8211-8220)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a scheduled task starts.
    /// </summary>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Information,
        Message = "Scheduled task started: '{taskName}' (ExecutionId: {executionId})")]
    public static partial IGenericMessage TaskStarted(
        ILogger logger,
        string taskName,
        Guid executionId);

    /// <summary>
    /// Logs when a scheduled task completes successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Information,
        Message = "Scheduled task completed: '{taskName}' (ExecutionId: {executionId}, Duration: {durationMs}ms)")]
    public static partial IGenericMessage TaskCompleted(
        ILogger logger,
        string taskName,
        Guid executionId,
        double durationMs);

    /// <summary>
    /// Logs when a task is being scheduled.
    /// </summary>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Information,
        Message = "Scheduling task '{taskName}' with trigger '{triggerType}'")]
    public static partial IGenericMessage SchedulingTask(
        ILogger logger,
        string taskName,
        string triggerType);

    /// <summary>
    /// Logs when a task is rescheduled.
    /// </summary>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Information,
        Message = "Task '{taskName}' rescheduled with new trigger")]
    public static partial IGenericMessage TaskRescheduled(
        ILogger logger,
        string taskName);

    /// <summary>
    /// Logs when a task is paused.
    /// </summary>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Information,
        Message = "Task '{taskName}' paused")]
    public static partial IGenericMessage TaskPaused(
        ILogger logger,
        string taskName);

    /// <summary>
    /// Logs when a task is resumed.
    /// </summary>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Information,
        Message = "Task '{taskName}' resumed")]
    public static partial IGenericMessage TaskResumed(
        ILogger logger,
        string taskName);

    /// <summary>
    /// Logs when a task is cancelled.
    /// </summary>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Information,
        Message = "Task '{taskName}' cancelled")]
    public static partial IGenericMessage TaskCancelled(
        ILogger logger,
        string taskName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Trigger Events (8221-8230)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a trigger fires.
    /// </summary>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Debug,
        Message = "Trigger fired for task '{taskName}' (TriggerType: {triggerType})")]
    public static partial IGenericMessage TriggerFired(
        ILogger logger,
        string taskName,
        string triggerType);

    /// <summary>
    /// Logs when a trigger is registered.
    /// </summary>
    [MessageLogging(
        EventId = 11016,
        Level = LogLevel.Information,
        Message = "Trigger registered: '{triggerName}' (Type: {triggerType})")]
    public static partial IGenericMessage TriggerRegistered(
        ILogger logger,
        string triggerName,
        string triggerType);

    /// <summary>
    /// Logs the next fire time for a trigger.
    /// </summary>
    [MessageLogging(
        EventId = 11017,
        Level = LogLevel.Debug,
        Message = "Next fire time for '{taskName}': {nextFireTime}")]
    public static partial IGenericMessage NextFireTime(
        ILogger logger,
        string taskName,
        DateTimeOffset nextFireTime);

    // ═══════════════════════════════════════════════════════════════════════════
    // CRUD Events (8231-8240)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a schedule is created.
    /// </summary>
    [MessageLogging(
        EventId = 11018,
        Level = LogLevel.Information,
        Message = "Schedule created: '{scheduleName}' for process '{processId}'")]
    public static partial IGenericMessage ScheduleCreated(
        ILogger logger,
        string scheduleName,
        string processId);

    /// <summary>
    /// Logs when a schedule is deleted.
    /// </summary>
    [MessageLogging(
        EventId = 11019,
        Level = LogLevel.Information,
        Message = "Schedule deleted: '{scheduleName}'")]
    public static partial IGenericMessage ScheduleDeleted(
        ILogger logger,
        string scheduleName);

    /// <summary>
    /// Logs when a schedule is updated.
    /// </summary>
    [MessageLogging(
        EventId = 11020,
        Level = LogLevel.Information,
        Message = "Schedule updated: '{scheduleName}'")]
    public static partial IGenericMessage ScheduleUpdated(
        ILogger logger,
        string scheduleName);

    /// <summary>
    /// Logs when schedules are loaded from the database.
    /// </summary>
    [MessageLogging(
        EventId = 11021,
        Level = LogLevel.Debug,
        Message = "Loaded {count} schedules")]
    public static partial IGenericMessage SchedulesLoaded(
        ILogger logger,
        int count);

    /// <summary>
    /// Logs when a schedule is not found.
    /// </summary>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Warning,
        Message = "Schedule not found: '{scheduleName}'")]
    public static partial IGenericMessage ScheduleNotFound(
        ILogger logger,
        string scheduleName);

    /// <summary>
    /// Logs when a schedule is paused.
    /// </summary>
    [MessageLogging(
        EventId = 11022,
        Level = LogLevel.Information,
        Message = "Schedule paused: '{scheduleName}'")]
    public static partial IGenericMessage SchedulePaused(
        ILogger logger,
        string scheduleName);

    /// <summary>
    /// Logs when a schedule is resumed.
    /// </summary>
    [MessageLogging(
        EventId = 11023,
        Level = LogLevel.Information,
        Message = "Schedule resumed: '{scheduleName}'")]
    public static partial IGenericMessage ScheduleResumed(
        ILogger logger,
        string scheduleName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error Events (8241-8260)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a scheduling configuration is not found.
    /// </summary>
    [MessageLogging(
        EventId = 31001,
        Level = LogLevel.Error,
        Message = "Scheduling configuration not found: '{configurationName}'")]
    public static partial IGenericMessage ConfigurationNotFound(
        ILogger logger,
        string configurationName);

    /// <summary>
    /// Logs when configuration loading fails.
    /// </summary>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "Failed to load configuration for scheduling '{configurationName}' (type: {schedulingType})")]
    public static partial IGenericMessage ConfigurationLoadFailed(
        ILogger logger,
        string configurationName,
        string schedulingType);

    /// <summary>
    /// Logs when no factory is registered for a scheduling type.
    /// </summary>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Error,
        Message = "No factory registered for scheduling type '{schedulingType}'")]
    public static partial IGenericMessage NoFactoryRegistered(
        ILogger logger,
        string schedulingType);

    /// <summary>
    /// Logs when scheduling service creation fails.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Failed to create scheduling service '{configurationName}': {error}")]
    public static partial IGenericMessage CreationFailed(
        ILogger logger,
        string configurationName,
        string error);

    /// <summary>
    /// Logs when scheduled task execution fails.
    /// </summary>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Task execution failed: '{taskName}' (ExecutionId: {executionId}) - {error}")]
    public static partial IGenericMessage TaskExecutionFailed(
        ILogger logger,
        string taskName,
        Guid executionId,
        string error);

    /// <summary>
    /// Logs when an exception occurs during scheduling operations.
    /// </summary>
    [MessageLogging(
        EventId = 91002,
        Level = LogLevel.Error,
        Message = "Exception during scheduling operation for '{configurationName}'")]
    public static partial IGenericMessage GetSchedulingException(
        ILogger logger,
        Exception exception,
        string configurationName);

    /// <summary>
    /// Logs when a type cast fails.
    /// </summary>
    [MessageLogging(
        EventId = 91003,
        Level = LogLevel.Warning,
        Message = "Scheduling service cast failed. Expected '{expectedType}', actual '{actualType}'")]
    public static partial IGenericMessage CastFailed(
        ILogger logger,
        string expectedType,
        string actualType);

    /// <summary>
    /// Logs when a trigger fails.
    /// </summary>
    [MessageLogging(
        EventId = 91004,
        Level = LogLevel.Error,
        Message = "Trigger failed for task '{taskName}': {error}")]
    public static partial IGenericMessage TriggerFailed(
        ILogger logger,
        string taskName,
        string error);

    /// <summary>
    /// Logs when scheduling a task fails.
    /// </summary>
    [MessageLogging(
        EventId = 91005,
        Level = LogLevel.Error,
        Message = "Failed to schedule task '{taskName}': {error}")]
    public static partial IGenericMessage SchedulingFailed(
        ILogger logger,
        string taskName,
        string error);

    // ═══════════════════════════════════════════════════════════════════════════
    // Diagnostic Events (8261-8280)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs scheduling configuration details for debugging.
    /// </summary>
    [MessageLogging(
        EventId = 11024,
        Level = LogLevel.Debug,
        Message = "Scheduling '{configurationName}' configuration - Type: {schedulingType}, Enabled: {isEnabled}")]
    public static partial IGenericMessage ConfigurationDetails(
        ILogger logger,
        string configurationName,
        string schedulingType,
        bool isEnabled);

    /// <summary>
    /// Logs task execution metrics for debugging.
    /// </summary>
    [MessageLogging(
        EventId = 11025,
        Level = LogLevel.Debug,
        Message = "Task metrics for '{taskName}' - Executions: {executionCount}, Successes: {successCount}, Failures: {failureCount}")]
    public static partial IGenericMessage TaskMetrics(
        ILogger logger,
        string taskName,
        int executionCount,
        int successCount,
        int failureCount);

    /// <summary>
    /// Logs provider state for debugging.
    /// </summary>
    [MessageLogging(
        EventId = 11026,
        Level = LogLevel.Debug,
        Message = "Scheduling provider state - Factories: {factoryCount}, Cached: {cachedCount}")]
    public static partial IGenericMessage ProviderState(
        ILogger logger,
        int factoryCount,
        int cachedCount);

    /// <summary>
    /// Logs the scheduler status.
    /// </summary>
    [MessageLogging(
        EventId = 11027,
        Level = LogLevel.Debug,
        Message = "Scheduler status - Running: {isRunning}, TaskCount: {taskCount}")]
    public static partial IGenericMessage SchedulerStatus(
        ILogger logger,
        bool isRunning,
        int taskCount);

    /// <summary>
    /// Logs that the scheduling factory received a null configuration.
    /// </summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "Scheduling factory invoked with null configuration")]
    public static partial IGenericMessage FactoryConfigurationNull(ILogger logger);

    /// <summary>
    /// Logs that the scheduling factory received a configuration of an unexpected type.
    /// </summary>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "Scheduling factory expected SchedulerConfiguration, received {actualType}")]
    public static partial IGenericMessage FactoryConfigurationTypeMismatch(
        ILogger logger,
        string actualType);

    /// <summary>
    /// Logs that the scheduling factory created a new service instance.
    /// </summary>
    [MessageLogging(
        EventId = 11028,
        Level = LogLevel.Information,
        Message = "Scheduling service created for scheduler '{schedulerName}'")]
    public static partial IGenericMessage FactoryServiceCreated(
        ILogger logger,
        string schedulerName);

    /// <summary>
    /// Traces a scheduling command translator type being constructed (compile-time discovery via [TypeOption]).
    /// </summary>
    [MessageLogging(
        EventId = 11029,
        Level = LogLevel.Trace,
        Message = "[SchedulingCommandTranslatorBase] Initializing translator '{translatorName}' for domain '{domainName}'")]
    public static partial IGenericMessage TranslatorInitializing(
        ILogger logger,
        string translatorName,
        string domainName);

    /// <summary>
    /// Logs the defect condition immediately before <see cref="SchedulingCommandTranslatorBase{TNative}"/>
    /// throws <see cref="ArgumentNullException"/> for a null or empty translator name.
    /// </summary>
    [MessageLogging(
        EventId = 91006,
        Level = LogLevel.Error,
        Message = "[SchedulingCommandTranslatorBase] Translator name is required and was null or empty")]
    public static partial IGenericMessage TranslatorNameMissing(ILogger logger);
}
