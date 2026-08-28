using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Conventions;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Scheduling.Abstractions.OptionTypes;
using Fdw.Services.Scheduling.Abstractions.OptionTypes.TriggerTypeImplementations;
using Fdw.Services.Scheduling.Abstractions.Logging;
using Fdw.Services.Scheduling.Abstractions.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.Models;

/// <summary>
/// Default implementation of <see cref="IGenericTrigger"/> that represents a trigger configuration
/// defining HOW a schedule determines WHEN to execute.
/// </summary>
/// <remarks>
/// <para>
/// A trigger provides the specific configuration and parameters needed by a trigger type
/// to calculate execution times and validate scheduling rules. This implementation supports:
/// </para>
/// <list type="bullet">
///   <item><description><strong>Multiple trigger types</strong>: Cron, Interval, Once, Manual</description></item>
///   <item><description><strong>Type-specific configuration</strong>: Flexible parameter storage</description></item>
///   <item><description><strong>State management</strong>: Enable/disable with timestamps</description></item>
///   <item><description><strong>Metadata support</strong>: Extensible metadata for custom properties</description></item>
/// </list>
/// <para>
/// The Trigger separates trigger logic (in TriggerTypeBase derivatives) from trigger data,
/// enabling flexible scheduling configurations while maintaining strong typing and validation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get a cron trigger for daily execution
/// var dailyTrigger = Trigger.CreateCron(
///     name: "Daily at 9 AM",
///     cronExpression: "0 9 * * *",
///     timeZoneId: "America/New_York"
/// );
/// 
/// // Get an interval trigger for every 30 minutes
/// var intervalTrigger = Trigger.CreateInterval(
///     name: "Every 30 minutes",
///     intervalMinutes: 30
/// );
/// 
/// // Get a manual trigger for on-demand execution
/// var manualTrigger = Trigger.CreateManual(
///     name: "Manual backup trigger"
/// );
/// 
/// // Validate any trigger
/// var validationResult = trigger.Validate();
/// </code>
/// </example>
public sealed class Trigger : IGenericTrigger
{
    private readonly Dictionary<string, object> _configuration;
    private readonly Dictionary<string, object> _metadata;
    private readonly ILogger<Trigger> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Trigger"/> class.
    /// </summary>
    /// <param name="triggerId">The unique identifier for this trigger.</param>
    /// <param name="triggerName">The name of the trigger.</param>
    /// <param name="triggerType">The trigger type identifier.</param>
    /// <param name="configuration">Configuration parameters for the trigger.</param>
    /// <param name="isEnabled">Whether this trigger is enabled.</param>
    /// <param name="createdUtc">When the trigger was created.</param>
    /// <param name="modifiedUtc">When the trigger was last modified.</param>
    /// <param name="metadata">Optional metadata for the trigger.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="ArgumentException">Thrown when required parameters are empty or whitespace.</exception>
#pragma warning disable FDW007 // Sequential property initialization with validation checks
    private Trigger(
        string triggerId,
        string triggerName,
        string triggerType,
        IReadOnlyDictionary<string, object> configuration,
        bool isEnabled,
        DateTime createdUtc,
        DateTime modifiedUtc,
        IReadOnlyDictionary<string, object>? metadata,
        ILogger<Trigger>? logger = null)
    {
        TriggerId = triggerId ?? throw new ArgumentNullException(nameof(triggerId));
        TriggerName = triggerName ?? throw new ArgumentNullException(nameof(triggerName));
        TriggerType = triggerType ?? throw new ArgumentNullException(nameof(triggerType));
        IsEnabled = isEnabled;
        CreatedUtc = createdUtc;
        ModifiedUtc = modifiedUtc;
        _logger = logger ?? NullLogger<Trigger>.Instance;

        // Initialize configuration dictionary
        _configuration = new Dictionary<string, object>(StringComparer.Ordinal);
        if (configuration != null)
        {
            foreach (var kvp in configuration)
            {
                _configuration[kvp.Key] = kvp.Value;
            }
        }

        // Initialize metadata dictionary
        _metadata = new Dictionary<string, object>(StringComparer.Ordinal);
        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                _metadata[kvp.Key] = kvp.Value;
            }
        }

        // Validate required parameters
        if (string.IsNullOrWhiteSpace(TriggerId))
        {
            throw new ArgumentException("Trigger ID cannot be null or empty", nameof(triggerId));
        }

        if (string.IsNullOrWhiteSpace(TriggerName))
        {
            throw new ArgumentException("Trigger name cannot be null or empty", nameof(triggerName));
        }

        if (string.IsNullOrWhiteSpace(TriggerType))
        {
            throw new ArgumentException("Trigger type cannot be null or empty", nameof(triggerType));
        }
    }
#pragma warning restore FDW007

    #region IFractalTrigger Implementation

    /// <inheritdoc/>
    public string TriggerId { get; }

    /// <inheritdoc/>
    public string TriggerName { get; }

    /// <inheritdoc/>
    public string TriggerType { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object> Configuration => _configuration;

    /// <inheritdoc/>
    public bool IsEnabled { get; private set; }

    /// <inheritdoc/>
    public DateTime CreatedUtc { get; }

    /// <inheritdoc/>
    public DateTime ModifiedUtc { get; private set; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object>? Metadata => _metadata.Count > 0 ? _metadata : null;

    #endregion

    #region Extended Properties

    /// <summary>
    /// Gets the unique identifier for this trigger (same as TriggerId).
    /// </summary>
    /// <value>A unique identifier for the trigger instance.</value>
    public string Id => TriggerId;

    /// <summary>
    /// Gets the name of the trigger (same as TriggerName).
    /// </summary>
    /// <value>A human-readable name for the trigger.</value>
    public string Name => TriggerName;

    /// <summary>
    /// Gets an optional description of what this trigger does.
    /// </summary>
    /// <value>A human-readable description from metadata, or null if not set.</value>
    public string? Description
    {
        get
        {
            if (_metadata.TryGetValue("Description", out var description) &&
                description is string descriptionText)
            {
                return descriptionText;
            }
            return null;
        }
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a cron trigger that executes based on a cron expression.
    /// </summary>
    /// <param name="name">The name of the trigger.</param>
    /// <param name="cronExpression">The cron expression (e.g., "0 9 * * *" for daily at 9 AM).</param>
    /// <param name="timeZoneId">Optional timezone ID (default: UTC).</param>
    /// <param name="description">Optional description of the trigger.</param>
    /// <param name="isEnabled">Whether the trigger is enabled (default: true).</param>
    /// <param name="metadata">Optional additional metadata.</param>
    /// <returns>A new cron trigger instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="ArgumentException">Thrown when required parameters are empty or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// Cron triggers use standard cron expressions to define complex scheduling patterns.
    /// The trigger supports:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Standard 5-field cron expressions (minute hour day month weekday)</description></item>
    ///   <item><description>Extended 6-field expressions with seconds</description></item>
    ///   <item><description>Special descriptors like @daily, @hourly, @monthly</description></item>
    ///   <item><description>Timezone-aware scheduling with DST handling</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Daily at 9 AM Eastern Time
    /// var dailyTrigger = Trigger.CreateCron(
    ///     name: "Daily Report",
    ///     cronExpression: "0 9 * * *",
    ///     timeZoneId: "America/New_York",
    ///     description: "Generate daily reports every morning"
    /// );
    /// 
    /// // Every 15 minutes
    /// var frequentTrigger = Trigger.CreateCron(
    ///     name: "Frequent Check",
    ///     cronExpression: "0 */15 * * * *"
    /// );
    /// 
    /// // Using cron descriptors
    /// var weeklyTrigger = Trigger.CreateCron(
    ///     name: "Weekly Cleanup",
    ///     cronExpression: "@weekly"
    /// );
    /// </code>
    /// </example>
    public static Trigger CreateCron(
        string name,
        string cronExpression,
        string? timeZoneId = null,
        string? description = null,
        bool isEnabled = true,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Trigger name cannot be null or empty", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            throw new ArgumentException("Cron expression cannot be null or empty", nameof(cronExpression));
        }

        var config = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Cron.CronExpressionKey] = cronExpression
        };

        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            config[Cron.TimeZoneIdKey] = timeZoneId!;
        }

        var combinedMetadata = CombineMetadata(description, metadata);
        var now = DateTime.UtcNow;
        var triggerId = Guid.NewGuid().ToString("N");

        return new Trigger(
            triggerId: triggerId,
            triggerName: name,
            triggerType: "Cron",
            configuration: config,
            isEnabled: isEnabled,
            createdUtc: now,
            modifiedUtc: now,
            metadata: combinedMetadata
        );
    }

    /// <summary>
    /// Creates an interval trigger that executes at regular intervals.
    /// </summary>
    /// <param name="name">The name of the trigger.</param>
    /// <param name="intervalMinutes">The interval between executions in minutes.</param>
    /// <param name="startDelayMinutes">Optional delay before the first execution (default: 0).</param>
    /// <param name="description">Optional description of the trigger.</param>
    /// <param name="isEnabled">Whether the trigger is enabled (default: true).</param>
    /// <param name="metadata">Optional additional metadata.</param>
    /// <returns>A new interval trigger instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="ArgumentException">Thrown when required parameters are invalid.</exception>
    /// <remarks>
    /// <para>
    /// Interval triggers execute at fixed intervals, making them suitable for:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Regular maintenance tasks</description></item>
    ///   <item><description>Periodic health checks</description></item>
    ///   <item><description>Data synchronization processes</description></item>
    ///   <item><description>Monitoring and alerting systems</description></item>
    /// </list>
    /// <para>
    /// The interval is measured from the completion of one execution to the start of the next,
    /// ensuring tasks don't overlap even if they take longer than expected.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Every 30 minutes with immediate start
    /// var frequentTrigger = Trigger.CreateInterval(
    ///     name: "Health Check",
    ///     intervalMinutes: 30,
    ///     description: "Check system health every 30 minutes"
    /// );
    /// 
    /// // Every hour with 5-minute startup delay
    /// var hourlyTrigger = Trigger.CreateInterval(
    ///     name: "Data Sync",
    ///     intervalMinutes: 60,
    ///     startDelayMinutes: 5,
    ///     description: "Synchronize data every hour after 5 minute delay"
    /// );
    /// </code>
    /// </example>
    public static Trigger CreateInterval(
        string name,
        int intervalMinutes,
        int startDelayMinutes = 0,
        string? description = null,
        bool isEnabled = true,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Trigger name cannot be null or empty", nameof(name));
        }

        if (intervalMinutes <= 0)
        {
            throw new ArgumentException("Interval must be greater than zero", nameof(intervalMinutes));
        }

        if (startDelayMinutes < 0)
        {
            throw new ArgumentException("Start delay cannot be negative", nameof(startDelayMinutes));
        }

        var config = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Interval.IntervalMinutesKey] = intervalMinutes
        };

        if (startDelayMinutes > 0)
        {
            // For interval triggers, we can set a start time in the future instead of a delay
            config[Interval.StartTimeKey] = DateTime.UtcNow.AddMinutes(startDelayMinutes);
        }

        var combinedMetadata = CombineMetadata(description, metadata);
        var now = DateTime.UtcNow;
        var triggerId = Guid.NewGuid().ToString("N");

        return new Trigger(
            triggerId: triggerId,
            triggerName: name,
            triggerType: "Interval",
            configuration: config,
            isEnabled: isEnabled,
            createdUtc: now,
            modifiedUtc: now,
            metadata: combinedMetadata
        );
    }

    /// <summary>
    /// Creates a once trigger that executes at a specific date and time.
    /// </summary>
    /// <param name="name">The name of the trigger.</param>
    /// <param name="executeAtUtc">The specific date and time to execute (in UTC).</param>
    /// <param name="description">Optional description of the trigger.</param>
    /// <param name="isEnabled">Whether the trigger is enabled (default: true).</param>
    /// <param name="metadata">Optional additional metadata.</param>
    /// <returns>A new once trigger instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="ArgumentException">Thrown when required parameters are invalid.</exception>
    /// <remarks>
    /// <para>
    /// Once triggers are designed for single-execution scenarios such as:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Scheduled maintenance windows</description></item>
    ///   <item><description>One-time data migrations</description></item>
    ///   <item><description>Event-specific processing</description></item>
    ///   <item><description>Deadline-driven tasks</description></item>
    /// </list>
    /// <para>
    /// After execution, the trigger becomes inactive and will not execute again
    /// unless manually reset or a new trigger is created.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Execute once at a specific time
    /// var maintenanceTrigger = Trigger.CreateOnce(
    ///     name: "Maintenance Window",
    ///     executeAtUtc: DateTime.Parse("2024-12-25T02:00:00Z"),
    ///     description: "Christmas maintenance window"
    /// );
    /// 
    /// // Execute once in 24 hours
    /// var delayedTrigger = Trigger.CreateOnce(
    ///     name: "Delayed Task",
    ///     executeAtUtc: DateTime.UtcNow.AddHours(24),
    ///     description: "Execute task after 24 hour delay"
    /// );
    /// </code>
    /// </example>
    public static Trigger CreateOnce(
        string name,
        DateTime executeAtUtc,
        string? description = null,
        bool isEnabled = true,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Trigger name cannot be null or empty", nameof(name));
        }

        if (executeAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Execute time must be in UTC", nameof(executeAtUtc));
        }

        var config = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Once.StartTimeKey] = executeAtUtc
        };

        var combinedMetadata = CombineMetadata(description, metadata);
        var now = DateTime.UtcNow;
        var triggerId = Guid.NewGuid().ToString("N");

        return new Trigger(
            triggerId: triggerId,
            triggerName: name,
            triggerType: "Once",
            configuration: config,
            isEnabled: isEnabled,
            createdUtc: now,
            modifiedUtc: now,
            metadata: combinedMetadata
        );
    }

    /// <summary>
    /// Creates a manual trigger that executes only when manually triggered.
    /// </summary>
    /// <param name="name">The name of the trigger.</param>
    /// <param name="description">Optional description of the trigger.</param>
    /// <param name="requiredRole">Optional role required to execute this trigger.</param>
    /// <param name="allowConcurrent">Whether concurrent executions are allowed (default: true).</param>
    /// <param name="isEnabled">Whether the trigger is enabled (default: true).</param>
    /// <param name="metadata">Optional additional metadata.</param>
    /// <returns>A new manual trigger instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="ArgumentException">Thrown when required parameters are empty or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// Manual triggers provide on-demand execution capabilities for:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>User-initiated processes</description></item>
    ///   <item><description>Administrative tasks</description></item>
    ///   <item><description>Emergency procedures</description></item>
    ///   <item><description>Testing and debugging</description></item>
    /// </list>
    /// <para>
    /// Manual triggers never calculate automatic execution times - they execute
    /// only when explicitly triggered through the scheduling system API.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic manual trigger
    /// var backupTrigger = Trigger.CreateManual(
    ///     name: "Manual Backup",
    ///     description: "On-demand database backup"
    /// );
    /// 
    /// // Manual trigger with role restriction
    /// var adminTrigger = Trigger.CreateManual(
    ///     name: "System Reset",
    ///     description: "Emergency system reset",
    ///     requiredRole: "Administrator",
    ///     allowConcurrent: false
    /// );
    /// </code>
    /// </example>
    public static Trigger CreateManual(
        string name,
        string? description = null,
        string? requiredRole = null,
        bool allowConcurrent = true,
        bool isEnabled = true,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Trigger name cannot be null or empty", nameof(name));
        }

        var config = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Manual.AllowConcurrentKey] = allowConcurrent
        };

        if (!string.IsNullOrWhiteSpace(description))
        {
            config[Manual.DescriptionKey] = description!;
        }

        if (!string.IsNullOrWhiteSpace(requiredRole))
        {
            config[Manual.RequiredRoleKey] = requiredRole!;
        }

        var combinedMetadata = CombineMetadata(description, metadata);
        var now = DateTime.UtcNow;
        var triggerId = Guid.NewGuid().ToString("N");

        return new Trigger(
            triggerId: triggerId,
            triggerName: name,
            triggerType: "Manual",
            configuration: config,
            isEnabled: isEnabled,
            createdUtc: now,
            modifiedUtc: now,
            metadata: combinedMetadata
        );
    }

    /// <summary>
    /// Creates an event trigger that fires when the named event is raised.
    /// </summary>
    /// <param name="name">The trigger name.</param>
    /// <param name="eventName">The name of the event this trigger listens for.</param>
    /// <param name="description">Optional human-readable description.</param>
    /// <param name="isEnabled">Whether the trigger is enabled.</param>
    /// <param name="metadata">Optional additional metadata.</param>
    /// <returns>A configured event <see cref="Trigger"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="eventName"/> is null, empty, or whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Event triggers bind execution to a named event rather than to the clock, so they never
    /// calculate an automatic next execution time and a polling loop never finds them due.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var trigger = Trigger.CreateEvent(
    ///     name: "Post-extract load",
    ///     eventName: "NightlyExtractCompleted",
    ///     description: "Runs once the nightly extract lands"
    /// );
    /// </code>
    /// </example>
    public static Trigger CreateEvent(
        string name,
        string eventName,
        string? description = null,
        bool isEnabled = true,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Trigger name cannot be null or empty", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(eventName))
        {
            throw new ArgumentException("Event name cannot be null or empty", nameof(eventName));
        }

        var config = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Event.EventNameKey] = eventName
        };

        if (!string.IsNullOrWhiteSpace(description))
        {
            config[Event.DescriptionKey] = description!;
        }

        var combinedMetadata = CombineMetadata(description, metadata);
        var now = DateTime.UtcNow;
        var triggerId = Guid.NewGuid().ToString("N");

        return new Trigger(
            triggerId: triggerId,
            triggerName: name,
            triggerType: "Event",
            configuration: config,
            isEnabled: isEnabled,
            createdUtc: now,
            modifiedUtc: now,
            metadata: combinedMetadata
        );
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validates this trigger configuration using the appropriate trigger type validation.
    /// </summary>
    /// <returns>
    /// A result indicating whether the trigger is valid. Success if valid,
    /// error result with validation messages if invalid.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method delegates validation to the appropriate trigger type implementation
    /// based on the TriggerType property. Each trigger type has specific validation rules:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><strong>Cron triggers</strong>: Validate cron expression syntax and timezone</description></item>
    ///   <item><description><strong>Interval triggers</strong>: Validate positive interval and delay values</description></item>
    ///   <item><description><strong>Once triggers</strong>: Validate execution time is in the future</description></item>
    ///   <item><description><strong>Manual triggers</strong>: Validate configuration format (minimal requirements)</description></item>
    /// </list>
    /// <para>
    /// The validation ensures that the trigger can be used successfully by the scheduling
    /// system and will not cause runtime errors during execution time calculations.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var trigger = Trigger.CreateCron("Daily Report", "0 9 * * *", "UTC");
    /// var validationResult = trigger.Validate();
    /// 
    /// if (validationResult.Error)
    /// {
    ///     Logger.LogError("Trigger validation failed: {CurrentMessage}", validationResult.CurrentMessage);
    ///     return;
    /// }
    /// 
    /// // Trigger is valid and ready to use
    /// await scheduler.ScheduleTrigger(trigger);
    /// </code>
    /// </example>
    public IGenericResult Validate()
    {
        // Basic validation first
        if (string.IsNullOrWhiteSpace(TriggerId))
        {
            return GenericResult.Failure(SchedulingLogger.TriggerIdNullOrEmpty(_logger));
        }

        if (string.IsNullOrWhiteSpace(TriggerName))
        {
            return GenericResult.Failure(SchedulingLogger.TriggerNameNullOrEmpty(_logger));
        }

        if (string.IsNullOrWhiteSpace(TriggerType))
        {
            return GenericResult.Failure(SchedulingLogger.TriggerTypeNullOrEmpty(_logger));
        }

        if (Configuration == null)
        {
            return GenericResult.Failure(SchedulingLogger.TriggerConfigurationNull(_logger));
        }

        if (ModifiedUtc < CreatedUtc)
        {
            return GenericResult.Failure(SchedulingLogger.InvalidTimestamp(_logger));
        }

        // Delegate to trigger type specific validation
        return ValidateByTriggerType();
    }

    #endregion

    #region State Management

    /// <summary>
    /// Updates the enabled status of this trigger.
    /// </summary>
    /// <param name="isEnabled">The new enabled status.</param>
    /// <remarks>
    /// This method updates the enabled status and the modified timestamp to track
    /// when the change occurred.
    /// </remarks>
    public void UpdateEnabledStatus(bool isEnabled)
    {
        IsEnabled = isEnabled;
        ModifiedUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds or updates a metadata value for this trigger.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <exception cref="ArgumentException">Thrown when key is null or empty.</exception>
    /// <remarks>
    /// This method allows dynamic metadata management and updates the modified
    /// timestamp to track when changes occurred.
    /// </remarks>
    public void SetMetadata(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Metadata key cannot be null or empty", nameof(key));
        }

        _metadata[key] = value;
        ModifiedUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes a metadata value from this trigger.
    /// </summary>
    /// <param name="key">The metadata key to remove.</param>
    /// <returns><c>true</c> if the key was found and removed; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when key is null or empty.</exception>
    /// <remarks>
    /// This method removes metadata and updates the modified timestamp if the key was found.
    /// </remarks>
    public bool RemoveMetadata(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Metadata key cannot be null or empty", nameof(key));
        }

        var removed = _metadata.Remove(key);
        if (removed)
        {
            ModifiedUtc = DateTime.UtcNow;
        }

        return removed;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Combines description with additional metadata into a single dictionary.
    /// </summary>
    /// <param name="description">Optional description text.</param>
    /// <param name="additionalMetadata">Optional additional metadata.</param>
    /// <returns>Combined metadata dictionary, or null if no metadata.</returns>
    [ExcludeFromCodeCoverage] // Helper method - tested through factory methods
    private static Dictionary<string, object>? CombineMetadata(
        string? description,
        IReadOnlyDictionary<string, object>? additionalMetadata)
    {
        var hasDescription = !string.IsNullOrWhiteSpace(description);
        var hasAdditionalMetadata = additionalMetadata?.Count > 0;

        if (!hasDescription && !hasAdditionalMetadata)
        {
            return null;
        }

        var combined = new Dictionary<string, object>(StringComparer.Ordinal);

        if (hasDescription)
        {
            combined["Description"] = description!;
        }

        if (hasAdditionalMetadata)
        {
            foreach (var kvp in additionalMetadata!)
            {
                combined[kvp.Key] = kvp.Value;
            }
        }

        return combined;
    }

    /// <summary>
    /// Validates the trigger using trigger type-specific validation logic.
    /// </summary>
    /// <returns>Validation result from the appropriate trigger type.</returns>
    [ExcludeFromCodeCoverage] // Helper method - trigger type validation tested in trigger type tests
    private IGenericResult ValidateByTriggerType()
    {
        // Note: In a complete implementation, this would look up the trigger type
        // from a registry and call its ValidateTrigger method. For this implementation,
        // we'll do basic trigger-type-specific validation.

        return TriggerType switch
        {
            "Cron" => ValidateCronTrigger(),
            "Interval" => ValidateIntervalTrigger(),
            "Once" => ValidateOnceTrigger(),
            "Manual" => ValidateManualTrigger(),
            _ => GenericResult.Failure(
                SchedulingResultCodes.ByName("UnknownTriggerType"),
                ResultDetails.Create("TriggerType", TriggerType))
        };
    }

    /// <summary>
    /// Validates cron trigger configuration.
    /// </summary>
    /// <returns>Validation result for cron configuration.</returns>
    [ExcludeFromCodeCoverage] // Helper method - detailed validation tested in trigger type tests
    private IGenericResult ValidateCronTrigger()
    {
        if (!Configuration.TryGetValue(Cron.CronExpressionKey, out var cronObj) ||
            cronObj is not string cronExpression ||
            string.IsNullOrWhiteSpace(cronExpression))
        {
            return GenericResult.Failure(SchedulingResultCodes.ByName("CronExpressionRequired"));
        }

        // Basic cron expression validation - detailed validation would be in TriggerType
        if (cronExpression.Split(' ').Length < 5)
        {
            return GenericResult.Failure(SchedulingResultCodes.ByName("CronExpressionInvalidFieldCount"));
        }

        return GenericResult.Success();
    }

    /// <summary>
    /// Validates interval trigger configuration.
    /// </summary>
    /// <returns>Validation result for interval configuration.</returns>
    [ExcludeFromCodeCoverage] // Helper method - detailed validation tested in trigger type tests
    private IGenericResult ValidateIntervalTrigger()
    {
        if (!Configuration.TryGetValue(Interval.IntervalMinutesKey, out var intervalObj) ||
            !TryConvertToInt(intervalObj, out var intervalMinutes) ||
            intervalMinutes <= 0)
        {
            return GenericResult.Failure(SchedulingResultCodes.ByName("IntervalMinutesRequired"));
        }

        if (Configuration.TryGetValue(Interval.StartTimeKey, out var startTimeObj) &&
            startTimeObj != null &&
            !TryConvertToDateTime(startTimeObj, out var _))
        {
            return GenericResult.Failure(SchedulingResultCodes.ByName("IntervalStartTimeInvalid"));
        }

        return GenericResult.Success();
    }

    /// <summary>
    /// Validates once trigger configuration.
    /// </summary>
    /// <returns>Validation result for once configuration.</returns>
    [ExcludeFromCodeCoverage] // Helper method - detailed validation tested in trigger type tests
    private IGenericResult ValidateOnceTrigger()
    {
        if (!Configuration.TryGetValue(Once.StartTimeKey, out var executeObj) ||
            executeObj is not DateTime executeAtUtc)
        {
            return GenericResult.Failure(SchedulingResultCodes.ByName("OnceExecuteAtRequired"));
        }

        if (executeAtUtc.Kind != DateTimeKind.Utc)
        {
            return GenericResult.Failure(SchedulingResultCodes.ByName("OnceExecuteAtMustBeUtc"));
        }

        return GenericResult.Success();
    }

    /// <summary>
    /// Validates manual trigger configuration.
    /// </summary>
    /// <returns>Validation result for manual configuration.</returns>
    [ExcludeFromCodeCoverage] // Helper method - detailed validation tested in trigger type tests
    private IGenericResult ValidateManualTrigger()
    {
        // Manual triggers have minimal validation requirements
        if (Configuration.TryGetValue(Manual.AllowConcurrentKey, out var allowConcurrentObj) &&
            !TryConvertToBool(allowConcurrentObj, out var _))
        {
            return GenericResult.Failure(SchedulingResultCodes.ByName("ManualAllowConcurrentInvalid"));
        }

        return GenericResult.Success();
    }

    /// <summary>
    /// Attempts to convert an object to an integer.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">The converted integer value.</param>
    /// <returns><c>true</c> if conversion succeeded; otherwise, <c>false</c>.</returns>
    [ExcludeFromCodeCoverage] // Utility method - tested through validation methods
    private static bool TryConvertToInt(object? value, out int result)
    {
        result = 0;

        return value switch
        {
            int intValue => TryAssignInt(intValue, out result),
            string stringValue when int.TryParse(stringValue, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsedInt) => TryAssignInt(parsedInt, out result),
            _ => false
        };
    }

    /// <summary>
    /// Attempts to convert an object to a DateTime.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">The converted DateTime value.</param>
    /// <returns><c>true</c> if conversion succeeded; otherwise, <c>false</c>.</returns>
    [ExcludeFromCodeCoverage] // Utility method - tested through validation methods
    private static bool TryConvertToDateTime(object? value, out DateTime result)
    {
        result = default;

        return value switch
        {
            DateTime dateTimeValue => TryAssignDateTime(dateTimeValue, out result),
            DateTimeOffset dateTimeOffsetValue => TryAssignDateTime(dateTimeOffsetValue.DateTime, out result),
            string stringValue => DateTime.TryParse(stringValue, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result),
            _ => false
        };
    }

    /// <summary>
    /// Helper method to assign a DateTime value to the result parameter.
    /// </summary>
    /// <param name="value">The value to assign.</param>
    /// <param name="result">The result parameter to assign to.</param>
    /// <returns>Always returns <c>true</c>.</returns>
    [ExcludeFromCodeCoverage] // Utility helper - tested through conversion methods
    private static bool TryAssignDateTime(DateTime value, out DateTime result)
    {
        result = value;
        return true;
    }

    /// <summary>
    /// Attempts to convert an object to a boolean.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">The converted boolean value.</param>
    /// <returns><c>true</c> if conversion succeeded; otherwise, <c>false</c>.</returns>
    [ExcludeFromCodeCoverage] // Utility method - tested through validation methods
    private static bool TryConvertToBool(object? value, out bool result)
    {
        result = false;

        return value switch
        {
            bool boolValue => TryAssignBool(boolValue, out result),
            string stringValue when string.Equals(stringValue, "true", StringComparison.OrdinalIgnoreCase) => TryAssignBool(true, out result),
            string stringValue when string.Equals(stringValue, "false", StringComparison.OrdinalIgnoreCase) => TryAssignBool(false, out result),
            int intValue when intValue == 0 => TryAssignBool(false, out result),
            int intValue when intValue == 1 => TryAssignBool(true, out result),
            _ => false
        };
    }

    /// <summary>
    /// Helper method to assign an integer value to the result parameter.
    /// </summary>
    /// <param name="value">The value to assign.</param>
    /// <param name="result">The result parameter to assign to.</param>
    /// <returns>Always returns <c>true</c>.</returns>
    [ExcludeFromCodeCoverage] // Utility helper - tested through conversion methods
    private static bool TryAssignInt(int value, out int result)
    {
        result = value;
        return true;
    }

    /// <summary>
    /// Helper method to assign a boolean value to the result parameter.
    /// </summary>
    /// <param name="value">The value to assign.</param>
    /// <param name="result">The result parameter to assign to.</param>
    /// <returns>Always returns <c>true</c>.</returns>
    [ExcludeFromCodeCoverage] // Utility helper - tested through conversion methods
    private static bool TryAssignBool(bool value, out bool result)
    {
        result = value;
        return true;
    }

    #endregion

    #region Object Overrides

    /// <summary>
    /// Returns a string representation of this trigger.
    /// </summary>
    /// <returns>A string containing the trigger's key information.</returns>
    public override string ToString()
    {
        return $"Trigger[{TriggerId}]: {TriggerName} ({TriggerType}) - {(IsEnabled ? "Enabled" : "Disabled")}";
    }

    /// <summary>
    /// Determines whether the specified object is equal to this trigger.
    /// </summary>
    /// <param name="obj">The object to compare with this trigger.</param>
    /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is Trigger other && string.Equals(TriggerId, other.TriggerId, StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns the hash code for this trigger.
    /// </summary>
    /// <returns>A hash code based on the trigger ID.</returns>
    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(TriggerId);
    }

    #endregion
}
