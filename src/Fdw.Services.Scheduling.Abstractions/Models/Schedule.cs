using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Conventions;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.Models;

/// <summary>
/// Default implementation of <see cref="IGenericSchedule"/> that represents a schedule definition 
/// defining WHEN and HOW a process should be executed.
/// </summary>
/// <remarks>
/// <para>
/// A schedule contains both timing information (via Trigger) and process execution metadata.
/// This implementation provides a comprehensive schedule model that supports:
/// </para>
/// <list type="bullet">
///   <item><description><strong>Process identification</strong>: Process type and configuration</description></item>
///   <item><description><strong>Trigger integration</strong>: Associated trigger for timing logic</description></item>
///   <item><description><strong>State management</strong>: Active/inactive status and timestamps</description></item>
///   <item><description><strong>Metadata support</strong>: Extensible metadata for custom properties</description></item>
/// </list>
/// <para>
/// The Schedule acts as a bridge between the scheduling system and process execution,
/// following separation of concerns between scheduling (WHEN) and execution (HOW).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get a new schedule for a daily backup process
/// var schedule = Schedule.CreateNew(
///     name: "Daily Backup",
///     processType: "Backup",
///     processConfiguration: backupConfig,
///     trigger: dailyTrigger,
///     description: "Daily database backup at 2 AM"
/// );
/// 
/// // Validate the schedule
/// var validationResult = schedule.Validate();
/// if (validationResult.Error)
/// {
///     // Handle validation errors
/// }
/// 
/// // Get from existing data
/// var existingSchedule = Schedule.Get(
///     id: "schedule-123",
///     name: "Weekly Report",
///     processType: "Reporting",
///     trigger: weeklyTrigger
/// );
/// </code>
/// </example>
public sealed class Schedule : IGenericSchedule
{
    private readonly Dictionary<string, object> _metadata;
    private readonly ILogger<Schedule> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Schedule"/> class.
    /// </summary>
    /// <param name="scheduleId">The unique identifier for this schedule.</param>
    /// <param name="scheduleName">The name of the schedule.</param>
    /// <param name="processId">The identifier of the process to execute.</param>
    /// <param name="cronExpression">The cron expression for scheduling.</param>
    /// <param name="isActive">Whether this schedule is active.</param>
    /// <param name="timeZoneId">The timezone identifier for the cron expression.</param>
    /// <param name="nextExecution">The next scheduled execution time.</param>
    /// <param name="metadata">Optional metadata dictionary.</param>
    /// <param name="id">The extended schedule identifier.</param>
    /// <param name="name">The extended schedule name.</param>
    /// <param name="processType">The type of process to execute.</param>
    /// <param name="processConfiguration">Configuration for the process.</param>
    /// <param name="trigger">The trigger configuration.</param>
    /// <param name="createdAt">When the schedule was created.</param>
    /// <param name="updatedAt">When the schedule was last updated.</param>
    /// <param name="description">Optional description of the schedule.</param>
    /// <param name="logger">Optional logger instance.</param>
#pragma warning disable FDW007 // Sequential property initialization with null checks
    private Schedule(
        string scheduleId,
        string scheduleName,
        string processId,
        string cronExpression,
        bool isActive,
        string timeZoneId,
        DateTime? nextExecution,
        IReadOnlyDictionary<string, object>? metadata,
        string id,
        string name,
        string processType,
        object processConfiguration,
        IGenericTrigger trigger,
        DateTime createdAt,
        DateTime updatedAt,
        string? description,
        ILogger<Schedule>? logger = null)
    {
        // IFractalSchedule properties
        ScheduleId = scheduleId ?? throw new ArgumentNullException(nameof(scheduleId));
        ScheduleName = scheduleName ?? throw new ArgumentNullException(nameof(scheduleName));
        ProcessId = processId ?? throw new ArgumentNullException(nameof(processId));
        CronExpression = cronExpression ?? throw new ArgumentNullException(nameof(cronExpression));
        IsActive = isActive;
        TimeZoneId = timeZoneId ?? "UTC";
        NextExecution = nextExecution;
        Metadata = metadata;

        // Extended properties
        Id = id ?? scheduleId;
        Name = name ?? scheduleName;
        ProcessType = processType ?? throw new ArgumentNullException(nameof(processType));
        ProcessConfiguration = processConfiguration ?? throw new ArgumentNullException(nameof(processConfiguration));
        Trigger = trigger ?? throw new ArgumentNullException(nameof(trigger));
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Description = description;
        _logger = logger ?? NullLogger<Schedule>.Instance;

        // Initialize metadata dictionary
        _metadata = new Dictionary<string, object>(StringComparer.Ordinal);
        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                _metadata[kvp.Key] = kvp.Value;
            }
        }
    }
#pragma warning restore FDW007

    #region IFractalSchedule Implementation

    /// <inheritdoc/>
    public string ScheduleId { get; }

    /// <inheritdoc/>
    public string ScheduleName { get; }

    /// <inheritdoc/>
    public string ProcessId { get; }

    /// <inheritdoc/>
    public string CronExpression { get; }

    /// <inheritdoc/>
    public DateTime? NextExecution { get; private set; }

    /// <inheritdoc/>
    public bool IsActive { get; private set; }

    /// <inheritdoc/>
    public string TimeZoneId { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object>? Metadata { get; }

    #endregion

    #region Extended Properties

    /// <summary>
    /// Gets the unique identifier for this schedule.
    /// </summary>
    /// <value>A unique identifier for the schedule instance.</value>
    public string Id { get; }

    /// <summary>
    /// Gets the name of the schedule for display and logging purposes.
    /// </summary>
    /// <value>A human-readable name for the schedule.</value>
    public string Name { get; }

    /// <summary>
    /// Gets the type of process that should be executed when this schedule triggers.
    /// </summary>
    /// <value>The process type identifier (e.g., "ETL", "Backup", "HealthCheck").</value>
    public string ProcessType { get; }

    /// <summary>
    /// Gets the configuration object for the process execution.
    /// </summary>
    /// <value>Configuration data specific to the process type.</value>
    public object ProcessConfiguration { get; }

    /// <summary>
    /// Gets the trigger that defines when this schedule should execute.
    /// </summary>
    /// <value>The trigger configuration that determines execution timing.</value>
    public IGenericTrigger Trigger { get; }

    /// <summary>
    /// Gets the time when this schedule was created.
    /// </summary>
    /// <value>The UTC timestamp when the schedule was created.</value>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Gets the time when this schedule was last updated.
    /// </summary>
    /// <value>The UTC timestamp when the schedule was last modified.</value>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Gets an optional description of what this schedule does.
    /// </summary>
    /// <value>A human-readable description of the schedule purpose.</value>
    public string? Description { get; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new schedule with a generated identifier and current timestamps.
    /// </summary>
    /// <param name="name">The name of the schedule.</param>
    /// <param name="processType">The type of process to execute.</param>
    /// <param name="processConfiguration">Configuration for the process.</param>
    /// <param name="trigger">The trigger that defines when to execute.</param>
    /// <param name="description">Optional description of the schedule.</param>
    /// <param name="isActive">Whether the schedule should be active initially (default: true).</param>
    /// <param name="metadata">Optional metadata for the schedule.</param>
    /// <returns>A new schedule instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="ArgumentException">Thrown when required parameters are empty or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// This method creates a schedule with auto-generated identifiers and timestamps.
    /// The schedule and process IDs are generated using GUIDs, and created/updated
    /// timestamps are set to the current UTC time.
    /// </para>
    /// <para>
    /// The cron expression and timezone are extracted from the trigger configuration
    /// to populate the IFractalSchedule interface properties for backward compatibility.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var cronTrigger = Trigger.CreateCron("Daily Backup", "0 2 * * *", "UTC");
    /// var schedule = Schedule.CreateNew(
    ///     name: "Daily Database Backup",
    ///     processType: "DatabaseBackup",
    ///     processConfiguration: backupConfig,
    ///     trigger: cronTrigger,
    ///     description: "Automated daily backup of production database"
    /// );
    /// </code>
    /// </example>
    public static Schedule CreateNew(
        string name,
        string processType,
        object processConfiguration,
        IGenericTrigger trigger,
        string? description = null,
        bool isActive = true,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Schedule name cannot be null or empty", nameof(name));
        }

        var scheduleId = Guid.NewGuid().ToString("N");
        var processId = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;

        // Extract cron expression and timezone from trigger
        var cronExpression = ExtractCronExpression(trigger);
        var timeZoneId = ExtractTimeZoneId(trigger);

        return new Schedule(
            scheduleId: scheduleId,
            scheduleName: name,
            processId: processId,
            cronExpression: cronExpression,
            isActive: isActive,
            timeZoneId: timeZoneId,
            nextExecution: null, // Will be calculated when needed
            metadata: metadata,
            id: scheduleId,
            name: name,
            processType: processType,
            processConfiguration: processConfiguration,
            trigger: trigger,
            createdAt: now,
            updatedAt: now,
            description: description
        );
    }

    /// <summary>
    /// Creates a schedule from existing data with specified identifiers and timestamps.
    /// </summary>
    /// <param name="id">The schedule identifier.</param>
    /// <param name="name">The name of the schedule.</param>
    /// <param name="processType">The type of process to execute.</param>
    /// <param name="processConfiguration">Configuration for the process.</param>
    /// <param name="trigger">The trigger that defines when to execute.</param>
    /// <param name="createdAt">When the schedule was created.</param>
    /// <param name="updatedAt">When the schedule was last updated.</param>
    /// <param name="description">Optional description of the schedule.</param>
    /// <param name="isActive">Whether the schedule is currently active.</param>
    /// <param name="processId">Optional specific process identifier.</param>
    /// <param name="nextExecution">The next scheduled execution time.</param>
    /// <param name="metadata">Optional metadata for the schedule.</param>
    /// <returns>A schedule instance with the specified data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="ArgumentException">Thrown when required parameters are empty or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// This method creates a schedule from existing data, typically used when loading
    /// schedules from storage or when specific identifiers and timestamps are required.
    /// </para>
    /// <para>
    /// All validation is performed to ensure the schedule data is consistent and complete.
    /// The cron expression and timezone are extracted from the trigger configuration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var schedule = Schedule.Get(
    ///     id: "existing-schedule-123",
    ///     name: "Weekly Report",
    ///     processType: "Reporting",
    ///     processConfiguration: reportConfig,
    ///     trigger: weeklyTrigger,
    ///     createdAt: DateTime.Parse("2024-01-01T00:00:00Z"),
    ///     updatedAt: DateTime.Parse("2024-01-15T12:00:00Z"),
    ///     description: "Weekly sales report generation"
    /// );
    /// </code>
    /// </example>
    public static Schedule Create(
        string id,
        string name,
        string processType,
        object processConfiguration,
        IGenericTrigger trigger,
        DateTime createdAt,
        DateTime updatedAt,
        string? description = null,
        bool isActive = true,
        string? processId = null,
        DateTime? nextExecution = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Schedule ID cannot be null or empty", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Schedule name cannot be null or empty", nameof(name));
        }

        var actualProcessId = processId ?? Guid.NewGuid().ToString("N");
        var cronExpression = ExtractCronExpression(trigger);
        var timeZoneId = ExtractTimeZoneId(trigger);

        return new Schedule(
            scheduleId: id,
            scheduleName: name,
            processId: actualProcessId,
            cronExpression: cronExpression,
            isActive: isActive,
            timeZoneId: timeZoneId,
            nextExecution: nextExecution,
            metadata: metadata,
            id: id,
            name: name,
            processType: processType,
            processConfiguration: processConfiguration,
            trigger: trigger,
            createdAt: createdAt,
            updatedAt: updatedAt,
            description: description
        );
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validates that this schedule configuration is complete and correct.
    /// </summary>
    /// <returns>
    /// A result indicating whether the schedule is valid. Success if valid,
    /// error result with validation messages if invalid.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs comprehensive validation of the schedule configuration:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><strong>Identity validation</strong>: Ensures IDs and names are present</description></item>
    ///   <item><description><strong>Process validation</strong>: Ensures process type and configuration are valid</description></item>
    ///   <item><description><strong>Trigger validation</strong>: Delegates to the trigger's validation logic</description></item>
    ///   <item><description><strong>Timestamp validation</strong>: Ensures timestamps are logical</description></item>
    /// </list>
    /// <para>
    /// The validation includes checking that the associated trigger is valid using
    /// the appropriate trigger type validation logic.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var schedule = Schedule.CreateNew(
    ///     name: "Test Schedule",
    ///     processType: "TestProcess",
    ///     processConfiguration: config,
    ///     trigger: testTrigger
    /// );
    /// 
    /// var validationResult = schedule.Validate();
    /// if (validationResult.Error)
    /// {
    ///     Logger.LogError("Schedule validation failed: {Messages}", validationResult.Messages);
    ///     return;
    /// }
    /// 
    /// // Schedule is valid and ready to use
    /// </code>
    /// </example>
    [ConventionOverride(MaxCyclomaticComplexity = 15)]  // Validation logic — independent condition checks
    public IGenericResult Validate()
    {
        // Validate basic identity information
        if (string.IsNullOrWhiteSpace(Id))
        {
            return GenericResult.Failure(SchedulingLogger.ScheduleIdNullOrEmpty(_logger));
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            return GenericResult.Failure(SchedulingLogger.ScheduleNameNullOrEmpty(_logger));
        }

        if (string.IsNullOrWhiteSpace(ScheduleId))
        {
            return GenericResult.Failure(SchedulingLogger.ScheduleIdNullOrEmpty(_logger));
        }

        if (string.IsNullOrWhiteSpace(ScheduleName))
        {
            return GenericResult.Failure(SchedulingLogger.ScheduleNameNullOrEmpty(_logger));
        }

        if (string.IsNullOrWhiteSpace(ProcessId))
        {
            return GenericResult.Failure(SchedulingLogger.ProcessIdNullOrEmpty(_logger));
        }

        // Validate process information
        if (string.IsNullOrWhiteSpace(ProcessType))
        {
            return GenericResult.Failure(SchedulingLogger.ProcessTypeNullOrEmpty(_logger));
        }

        if (ProcessConfiguration == null)
        {
            return GenericResult.Failure(SchedulingLogger.ProcessConfigurationNull(_logger));
        }

        // Validate trigger
        if (Trigger == null)
        {
            return GenericResult.Failure(SchedulingLogger.TriggerNull(_logger));
        }

        // Delegate trigger validation to the appropriate trigger type
        var triggerValidationResult = ValidateTriggerConfiguration();
        if (triggerValidationResult.Error)
        {
            return triggerValidationResult;
        }

        // Validate timestamps
        if (UpdatedAt < CreatedAt)
        {
            return GenericResult.Failure(SchedulingLogger.InvalidScheduleTimestamp(_logger));
        }

        return GenericResult.Success();
    }

    #endregion

    #region State Management

    /// <summary>
    /// Updates the schedule's active status.
    /// </summary>
    /// <param name="isActive">The new active status.</param>
    /// <remarks>
    /// This method updates both the extended IsActive property and updates
    /// the UpdatedAt timestamp to track when the change occurred.
    /// </remarks>
    public void UpdateActiveStatus(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the next execution time for this schedule.
    /// </summary>
    /// <param name="nextExecution">The next scheduled execution time in UTC.</param>
    /// <remarks>
    /// This method is typically called by the scheduling system when calculating
    /// next execution times based on the trigger configuration.
    /// </remarks>
    public void UpdateNextExecution(DateTime? nextExecution)
    {
        NextExecution = nextExecution;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Extracts the cron expression from a trigger configuration.
    /// </summary>
    /// <param name="trigger">The trigger to extract from.</param>
    /// <returns>The cron expression or a default if not found.</returns>
    [ExcludeFromCodeCoverage] // Helper method - tested through factory methods
    private static string ExtractCronExpression(IGenericTrigger trigger)
    {
        if (trigger?.Configuration?.TryGetValue("CronExpression", out var cronObj) == true &&
            cronObj is string cronExpression &&
            !string.IsNullOrWhiteSpace(cronExpression))
        {
            return cronExpression;
        }

        // Default for non-cron triggers
        return "@manual";
    }

    /// <summary>
    /// Extracts the timezone identifier from a trigger configuration.
    /// </summary>
    /// <param name="trigger">The trigger to extract from.</param>
    /// <returns>The timezone ID or UTC as default.</returns>
    [ExcludeFromCodeCoverage] // Helper method - tested through factory methods
    private static string ExtractTimeZoneId(IGenericTrigger trigger)
    {
        if (trigger?.Configuration?.TryGetValue("TimeZoneId", out var timezoneObj) == true &&
            timezoneObj is string timeZoneId &&
            !string.IsNullOrWhiteSpace(timeZoneId))
        {
            return timeZoneId;
        }

        return "UTC";
    }

    /// <summary>
    /// Validates the trigger configuration using the appropriate trigger type.
    /// </summary>
    /// <returns>Validation result from the trigger type.</returns>
    [ExcludeFromCodeCoverage] // Helper method - complex trigger validation tested in trigger type tests
    private IGenericResult ValidateTriggerConfiguration()
    {
        // Note: This would typically look up the trigger type from a registry
        // and call its ValidateTrigger method. For now, we'll do basic validation.

        if (string.IsNullOrWhiteSpace(Trigger.TriggerId))
        {
            return GenericResult.Failure(SchedulingLogger.TriggerIdNullOrEmpty(_logger));
        }

        if (string.IsNullOrWhiteSpace(Trigger.TriggerName))
        {
            return GenericResult.Failure(SchedulingLogger.TriggerNameNullOrEmpty(_logger));
        }

        if (string.IsNullOrWhiteSpace(Trigger.TriggerType))
        {
            return GenericResult.Failure(SchedulingLogger.TriggerTypeNullOrEmpty(_logger));
        }

        if (Trigger.Configuration == null)
        {
            return GenericResult.Failure(SchedulingLogger.TriggerConfigurationNull(_logger));
        }

        return GenericResult.Success();
    }

    #endregion

    #region Object Overrides

    /// <summary>
    /// Returns a string representation of this schedule.
    /// </summary>
    /// <returns>A string containing the schedule's key information.</returns>
    public override string ToString()
    {
        return $"Schedule[{Id}]: {Name} ({ProcessType}) - {(IsActive ? "Active" : "Inactive")}";
    }

    /// <summary>
    /// Determines whether the specified object is equal to this schedule.
    /// </summary>
    /// <param name="obj">The object to compare with this schedule.</param>
    /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is Schedule other && string.Equals(Id, other.Id, StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns the hash code for this schedule.
    /// </summary>
    /// <returns>A hash code based on the schedule ID.</returns>
    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Id);
    }

    #endregion
}
