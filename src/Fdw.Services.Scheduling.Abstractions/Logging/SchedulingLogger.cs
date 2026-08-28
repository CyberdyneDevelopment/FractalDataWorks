using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Scheduling.Abstractions.Logging;

/// <summary>
/// Static logger class for Scheduling operations using MessageLogging infrastructure.
/// </summary>
[MessageLoggingTypeCode("SCHEDULING")]
public static partial class SchedulingLogger
{
    /// <summary>
    /// Logs when trigger ID is null or empty.
    /// </summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error, Message = "Trigger ID cannot be null or empty")]
    public static partial IGenericMessage TriggerIdNullOrEmpty(ILogger logger);

    /// <summary>
    /// Logs when trigger name is null or empty.
    /// </summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Error, Message = "Trigger name cannot be null or empty")]
    public static partial IGenericMessage TriggerNameNullOrEmpty(ILogger logger);

    /// <summary>
    /// Logs when trigger type is null or empty.
    /// </summary>
    [MessageLogging(EventId = 21002, Level = LogLevel.Error, Message = "Trigger type cannot be null or empty")]
    public static partial IGenericMessage TriggerTypeNullOrEmpty(ILogger logger);

    /// <summary>
    /// Logs when trigger configuration is null.
    /// </summary>
    [MessageLogging(EventId = 21003, Level = LogLevel.Error, Message = "Trigger configuration cannot be null")]
    public static partial IGenericMessage TriggerConfigurationNull(ILogger logger);

    /// <summary>
    /// Logs when modified timestamp is earlier than created timestamp.
    /// </summary>
    [MessageLogging(EventId = 21004, Level = LogLevel.Error, Message = "Modified timestamp cannot be earlier than created timestamp")]
    public static partial IGenericMessage InvalidTimestamp(ILogger logger);

    /// <summary>
    /// Logs when schedule ID is null or empty.
    /// </summary>
    [MessageLogging(EventId = 21005, Level = LogLevel.Error, Message = "Schedule ID cannot be null or empty")]
    public static partial IGenericMessage ScheduleIdNullOrEmpty(ILogger logger);

    /// <summary>
    /// Logs when schedule name is null or empty.
    /// </summary>
    [MessageLogging(EventId = 21006, Level = LogLevel.Error, Message = "Schedule name cannot be null or empty")]
    public static partial IGenericMessage ScheduleNameNullOrEmpty(ILogger logger);

    /// <summary>
    /// Logs when process ID is null or empty.
    /// </summary>
    [MessageLogging(EventId = 21007, Level = LogLevel.Error, Message = "Process ID cannot be null or empty")]
    public static partial IGenericMessage ProcessIdNullOrEmpty(ILogger logger);

    /// <summary>
    /// Logs when process type is null or empty.
    /// </summary>
    [MessageLogging(EventId = 21008, Level = LogLevel.Error, Message = "Process type cannot be null or empty")]
    public static partial IGenericMessage ProcessTypeNullOrEmpty(ILogger logger);

    /// <summary>
    /// Logs when process configuration is null.
    /// </summary>
    [MessageLogging(EventId = 21009, Level = LogLevel.Error, Message = "Process configuration cannot be null")]
    public static partial IGenericMessage ProcessConfigurationNull(ILogger logger);

    /// <summary>
    /// Logs when trigger is null.
    /// </summary>
    [MessageLogging(EventId = 21010, Level = LogLevel.Error, Message = "Trigger cannot be null")]
    public static partial IGenericMessage TriggerNull(ILogger logger);

    /// <summary>
    /// Logs when schedule's updated timestamp is earlier than its created timestamp.
    /// </summary>
    [MessageLogging(EventId = 21011, Level = LogLevel.Error, Message = "Updated timestamp cannot be earlier than created timestamp")]
    public static partial IGenericMessage InvalidScheduleTimestamp(ILogger logger);

    /// <summary>
    /// Logs when a job has been successfully scheduled.
    /// </summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Information, Message = "Job has been scheduled successfully")]
    public static partial IGenericMessage JobScheduled(ILogger logger);

    /// <summary>
    /// Logs when a job has been successfully scheduled with job identifier.
    /// </summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information, Message = "Job '{jobId}' has been scheduled successfully")]
    public static partial IGenericMessage JobScheduled(ILogger logger, string jobId);

    /// <summary>
    /// Logs when a scheduled job execution has failed.
    /// </summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Scheduled job execution failed")]
    public static partial IGenericMessage JobFailed(ILogger logger);

    /// <summary>
    /// Logs when a scheduled job execution has failed with details.
    /// </summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error, Message = "Job '{jobId}' failed: {reason}")]
    public static partial IGenericMessage JobFailed(ILogger logger, string jobId, string reason);

    /// <summary>
    /// Logs when a scheduled job has completed successfully.
    /// </summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information, Message = "Scheduled job completed successfully")]
    public static partial IGenericMessage JobCompleted(ILogger logger);

    /// <summary>
    /// Logs when a scheduled job has completed successfully with details.
    /// </summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Information, Message = "Job '{jobId}' completed successfully in {durationMs}ms")]
    public static partial IGenericMessage JobCompleted(ILogger logger, string jobId, long durationMs);

    // =========================================================================
    // Trigger Validation Messages (4000-4099)
    // =========================================================================

    /// <summary>
    /// Logs when a configuration value must be a boolean.
    /// </summary>
    [MessageLogging(EventId = 21012, Level = LogLevel.Error, Message = "'{key}' configuration value must be a boolean if provided")]
    public static partial IGenericMessage ConfigurationValueMustBeBoolean(ILogger logger, string key);

    /// <summary>
    /// Logs when a configuration value must be a string.
    /// </summary>
    [MessageLogging(EventId = 21013, Level = LogLevel.Error, Message = "'{key}' configuration value must be a string if provided")]
    public static partial IGenericMessage ConfigurationValueMustBeString(ILogger logger, string key);

    /// <summary>
    /// Logs when a cron expression is required.
    /// </summary>
    [MessageLogging(EventId = 20000, Level = LogLevel.Warning, Message = "Cron expression is required and must be provided in the '{key}' configuration key")]
    public static partial IGenericMessage CronExpressionRequired(ILogger logger, string key);

    /// <summary>
    /// Logs when a cron expression has an invalid format.
    /// </summary>
    [MessageLogging(EventId = 21014, Level = LogLevel.Warning, Message = "Invalid cron expression format: {errorMessage}. Expression: '{expression}'")]
    public static partial IGenericMessage InvalidCronExpressionFormat(ILogger logger, string errorMessage, string expression);

    /// <summary>
    /// Logs when a cron expression is invalid.
    /// </summary>
    [MessageLogging(EventId = 21015, Level = LogLevel.Warning, Message = "Invalid cron expression: {errorMessage}. Expression: '{expression}'")]
    public static partial IGenericMessage InvalidCronExpression(ILogger logger, string errorMessage, string expression);

    /// <summary>
    /// Logs when a timezone identifier is invalid.
    /// </summary>
    [MessageLogging(EventId = 21016, Level = LogLevel.Warning, Message = "Invalid timezone identifier: '{timezoneId}'. Use standard timezone IDs like 'UTC', 'America/New_York', or 'Europe/London'")]
    public static partial IGenericMessage InvalidTimezoneIdentifier(ILogger logger, string timezoneId);

    /// <summary>
    /// Logs when a timezone configuration is invalid.
    /// </summary>
    [MessageLogging(EventId = 21017, Level = LogLevel.Error, Message = "Invalid timezone configuration: {errorMessage}. Timezone: '{timezoneId}'")]
    public static partial IGenericMessage InvalidTimezoneConfiguration(ILogger logger, string errorMessage, string timezoneId);

    /// <summary>
    /// Logs when a cron expression will never execute.
    /// </summary>
    [MessageLogging(EventId = 21018, Level = LogLevel.Warning, Message = "Cron expression '{expression}' will never execute. Verify the expression is not in the past or misconfigured")]
    public static partial IGenericMessage CronExpressionWillNeverExecute(ILogger logger, string expression);

    /// <summary>
    /// Logs when cron expression validation fails.
    /// </summary>
    [MessageLogging(EventId = 21019, Level = LogLevel.Error, Message = "Cron expression validation failed: {errorMessage}. Expression: '{expression}'")]
    public static partial IGenericMessage CronExpressionValidationFailed(ILogger logger, string errorMessage, string expression);

    /// <summary>
    /// Logs when a start time is invalid.
    /// </summary>
    [MessageLogging(EventId = 21020, Level = LogLevel.Error, Message = "Start time must be a valid DateTime if provided in the '{key}' configuration key")]
    public static partial IGenericMessage InvalidStartTime(ILogger logger, string key);

    /// <summary>
    /// Logs when a start time is too far in the past.
    /// </summary>
    [MessageLogging(EventId = 21021, Level = LogLevel.Warning, Message = "Start time is more than 1 day in the past and may not execute as expected. Start time: {startTime} UTC")]
    public static partial IGenericMessage StartTimeTooFarInPast(ILogger logger, string startTime);

    /// <summary>
    /// Logs when an interval is required.
    /// </summary>
    [MessageLogging(EventId = 21022, Level = LogLevel.Warning, Message = "Interval in minutes is required and must be provided in the '{key}' configuration key as a positive integer")]
    public static partial IGenericMessage IntervalRequired(ILogger logger, string key);

    /// <summary>
    /// Logs when an interval must be positive.
    /// </summary>
    [MessageLogging(EventId = 21023, Level = LogLevel.Warning, Message = "Interval must be greater than 0 minutes. Provided value: {value}")]
    public static partial IGenericMessage IntervalMustBePositive(ILogger logger, int value);

    /// <summary>
    /// Logs when a trigger type is unknown.
    /// </summary>
    [MessageLogging(EventId = 21024, Level = LogLevel.Warning, Message = "Unknown trigger type: {triggerType}")]
    public static partial IGenericMessage UnknownTriggerType(ILogger logger, string triggerType);

    /// <summary>
    /// Logs when a window cron expression is required.
    /// </summary>
    [MessageLogging(EventId = 21025, Level = LogLevel.Error, Message = "Window cron expression is required and must be provided in the '{key}' configuration key")]
    public static partial IGenericMessage WindowCronExpressionRequired(ILogger logger, string key);

    /// <summary>
    /// Logs when a window duration is required.
    /// </summary>
    [MessageLogging(EventId = 21026, Level = LogLevel.Error, Message = "Window duration in minutes is required and must be provided in the '{key}' configuration key as a positive integer")]
    public static partial IGenericMessage WindowDurationRequired(ILogger logger, string key);

    /// <summary>
    /// Logs when a window duration must be positive.
    /// </summary>
    [MessageLogging(EventId = 21027, Level = LogLevel.Warning, Message = "Window duration must be greater than 0 minutes. Provided value: {value}")]
    public static partial IGenericMessage WindowDurationMustBePositive(ILogger logger, int value);

    /// <summary>
    /// Logs when a retry interval is required.
    /// </summary>
    [MessageLogging(EventId = 21028, Level = LogLevel.Error, Message = "Retry interval in minutes is required and must be provided in the '{key}' configuration key as a positive integer")]
    public static partial IGenericMessage RetryIntervalRequired(ILogger logger, string key);

    /// <summary>
    /// Logs when a retry interval must be positive.
    /// </summary>
    [MessageLogging(EventId = 21029, Level = LogLevel.Warning, Message = "Retry interval must be greater than 0 minutes. Provided value: {value}")]
    public static partial IGenericMessage RetryIntervalMustBePositive(ILogger logger, int value);

    /// <summary>
    /// Logs when a manual trigger is asked to compute its next run time — which it cannot do.
    /// </summary>
    [MessageLogging(EventId = 41000, Level = LogLevel.Debug, Message = "Manual triggers do not have an automatic next run time; they execute only when explicitly triggered")]
    public static partial IGenericMessage ManualTriggerCannotComputeNextRunTime(ILogger logger);

    /// <summary>
    /// Logs when an event name is required.
    /// </summary>
    [MessageLogging(EventId = 21035, Level = LogLevel.Error, Message = "Event name is required and must be provided in the '{key}' configuration key as a non-empty string")]
    public static partial IGenericMessage EventNameRequired(ILogger logger, string key);

    /// <summary>
    /// Logs when an event trigger is asked to compute its next run time — which it cannot do.
    /// </summary>
    [MessageLogging(EventId = 41001, Level = LogLevel.Debug, Message = "Event triggers do not have an automatic next run time; they execute only when their named event is raised")]
    public static partial IGenericMessage EventTriggerCannotComputeNextRunTime(ILogger logger);

    // =========================================================================
    // Next-run Calculation Warning Messages (4021-4028)
    // These are Warning-level because CalculateNextExecution returns null on
    // failure; the caller degrades gracefully rather than propagating an error.
    // =========================================================================

    /// <summary>
    /// Logs when next run time calculation fails due to an invalid cron expression format.
    /// </summary>
    [MessageLogging(EventId = 21030, Level = LogLevel.Error, Message = "Failed to calculate next run time: invalid cron expression format. Expression: '{expression}'")]
    public static partial IGenericMessage CalculateNextRunCronFormatFailed(ILogger logger, Exception ex, string expression);

    /// <summary>
    /// Logs when next run time calculation fails due to an unrecognised timezone, falling back to UTC.
    /// The string parameter carries the cron expression or other available context for diagnostics.
    /// </summary>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error, Message = "Failed to calculate next run time with configured timezone, falling back to UTC. Context: '{context}'")]
    public static partial IGenericMessage CalculateNextRunTimeZoneFailed(ILogger logger, Exception ex, string context);

    /// <summary>
    /// Logs when next run time calculation fails during the UTC fallback attempt.
    /// </summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error, Message = "Failed to calculate next run time via UTC fallback")]
    public static partial IGenericMessage CalculateNextRunFallbackFailed(ILogger logger, Exception ex);

    /// <summary>
    /// Logs when next run time calculation fails due to an argument error.
    /// The string parameter carries the available diagnostic context (expression or key name).
    /// </summary>
    [MessageLogging(EventId = 21031, Level = LogLevel.Error, Message = "Failed to calculate next run time: argument error. Context: '{context}'")]
    public static partial IGenericMessage CalculateNextRunArgumentFailed(ILogger logger, Exception ex, string context);

    /// <summary>
    /// Logs when next run time calculation encounters a timezone error and no string context is available.
    /// </summary>
    [MessageLogging(EventId = 21032, Level = LogLevel.Error, Message = "Failed to calculate next run time: unrecognised timezone, returning null")]
    public static partial IGenericMessage CalculateNextRunTimeZoneError(ILogger logger, Exception ex);

    /// <summary>
    /// Logs when next run time calculation encounters an argument error and no string context is available.
    /// </summary>
    [MessageLogging(EventId = 21033, Level = LogLevel.Error, Message = "Failed to calculate next run time: argument error, returning null")]
    public static partial IGenericMessage CalculateNextRunArgumentError(ILogger logger, Exception ex);

    /// <summary>
    /// Logs when a timezone identifier is invalid, also capturing the originating exception.
    /// Used at catch sites where FDW022 requires the exception variable to be observed.
    /// </summary>
    [MessageLogging(EventId = 21034, Level = LogLevel.Error, Message = "Invalid timezone identifier '{timezoneId}': {exMessage}")]
    public static partial IGenericMessage InvalidTimezoneIdentifierWithException(ILogger logger, Exception ex, string timezoneId, string exMessage);

    /// <summary>
    /// Logs when a timezone or datetime conversion fails during validation and the check is skipped
    /// because it is not the primary validation for that field (the error was already reported).
    /// </summary>
    [MessageLogging(EventId = 91004, Level = LogLevel.Warning, Message = "Timezone/datetime conversion failed during secondary validation check; skipping start-time check")]
    public static partial IGenericMessage TimezoneConversionSkippedInValidation(ILogger logger, Exception ex);
}
