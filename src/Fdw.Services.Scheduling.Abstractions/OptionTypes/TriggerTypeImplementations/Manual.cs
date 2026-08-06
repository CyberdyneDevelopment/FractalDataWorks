using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Conventions;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes.TriggerTypeImplementations;

/// <summary>
/// Manual trigger type that executes only when manually triggered by a user or system.
/// </summary>
/// <remarks>
/// <para>
/// The Manual trigger type enables on-demand execution that is triggered explicitly
/// rather than automatically scheduled. It supports:
/// </para>
/// <list type="bullet">
///   <item><description>On-demand execution through external triggers</description></item>
///   <item><description>No automatic scheduling or time-based execution</description></item>
///   <item><description>Immediate execution when triggered</description></item>
///   <item><description>Optional metadata for trigger context and tracking</description></item>
/// </list>
/// <para>
/// Manual triggers never calculate automatic next execution times - they only execute
/// when explicitly triggered by user actions, API calls, or system events.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get a manual trigger (minimal configuration)
/// var manualConfig = new Dictionary&lt;string, object&gt;
/// {
///     { "Description", "Manual backup trigger" }
/// };
/// 
/// // With additional metadata
/// var manualConfig = new Dictionary&lt;string, object&gt;
/// {
///     { "Description", "Manual data processing trigger" },
///     { "RequiredRole", "DataProcessor" },
///     { "AllowConcurrent", false }
/// };
/// 
/// // Validate trigger (always succeeds for manual triggers)
/// var manualTrigger = TriggerTypes.Manual;
/// var validationResult = manualTrigger.ValidateTrigger(trigger);
/// var nextExecution = manualTrigger.CalculateNextExecution(trigger, null);
/// // nextExecution is always null for manual triggers
/// </code>
/// </example>
[TypeOption(typeof(TriggerTypes), "Manual", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class Manual : TriggerTypeBase
{
    private readonly ILogger<Manual> _logger;
    /// <summary>
    /// Configuration key for an optional description of the manual trigger.
    /// </summary>
    /// <remarks>
    /// Optional description that explains what the manual trigger does or when it should be used.
    /// This is purely informational and used for documentation and UI display purposes.
    /// </remarks>
    public const string DescriptionKey = "Description";

    /// <summary>
    /// Configuration key for specifying the required role or permission to execute this trigger.
    /// </summary>
    /// <remarks>
    /// Optional role or permission name that a user must have to manually execute this trigger.
    /// This provides basic access control for manual trigger execution.
    /// </remarks>
    public const string RequiredRoleKey = "RequiredRole";

    /// <summary>
    /// Configuration key for controlling whether concurrent executions are allowed.
    /// </summary>
    /// <remarks>
    /// Optional boolean flag indicating whether multiple concurrent executions of this
    /// manual trigger are allowed. Defaults to true if not specified.
    /// </remarks>
    public const string AllowConcurrentKey = "AllowConcurrent";

    /// <summary>
    /// Initializes a new instance of the <see cref="Manual"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    /// <remarks>
    /// Manual triggers do not require schedule persistence since they don't auto-schedule,
    /// and they execute immediately when triggered (they are immediate execution triggers).
    /// </remarks>
    public Manual(ILogger<Manual>? logger = null) : base(4, "Manual", requiresSchedule: false, isImmediate: true)
    {
        _logger = logger ?? NullLogger<Manual>.Instance;
    }

    /// <summary>
    /// Calculates the next execution time for manual triggers.
    /// </summary>
    /// <param name="trigger">The trigger configuration (not used for manual triggers).</param>
    /// <param name="lastExecution">The timestamp of the last execution (not used for manual triggers).</param>
    /// <returns>
    /// Always returns null, as manual triggers do not auto-schedule executions.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Manual triggers never calculate automatic next execution times because they are
    /// designed for on-demand execution only. They execute when:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>A user manually triggers them through a UI or API</description></item>
    ///   <item><description>A system component explicitly invokes them</description></item>
    ///   <item><description>An external event handler calls them</description></item>
    ///   <item><description>An administrative action starts them</description></item>
    /// </list>
    /// <para>
    /// The scheduling system should never automatically execute manual triggers based
    /// on time calculations - they are purely reactive to external trigger events.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Manual triggers never auto-schedule
    /// var nextExecution = manualTrigger.CalculateNextExecution(trigger, null);
    /// // nextExecution is always null
    /// 
    /// var nextExecution = manualTrigger.CalculateNextExecution(trigger, DateTime.UtcNow);
    /// // nextExecution is still always null
    /// 
    /// // Execution happens only when explicitly triggered:
    /// // scheduler.ExecuteManualTrigger(triggerId, userId);
    /// </code>
    /// </example>
    public override DateTime? CalculateNextExecution(IGenericTrigger trigger, DateTime? lastExecution)
    {
        // Manual triggers never auto-schedule - they execute only when manually triggered
        return null;
    }

    /// <summary>
    /// Validates that the trigger configuration is valid for manual triggers.
    /// </summary>
    /// <param name="trigger">The trigger configuration to validate.</param>
    /// <returns>
    /// Always returns a success result, as manual triggers have minimal validation requirements.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Manual trigger validation is very permissive since they are simple triggers that
    /// don't require complex configuration. The validation checks:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><strong>Configuration presence</strong>: Ensures basic configuration object exists</description></item>
    ///   <item><description><strong>Optional parameters</strong>: Validates format of optional configuration values</description></item>
    ///   <item><description><strong>No required parameters</strong>: Manual triggers work with minimal configuration</description></item>
    /// </list>
    /// <para>
    /// Even if optional parameters like Description or RequiredRole are malformed,
    /// the validation succeeds because these don't affect trigger execution functionality.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Empty configuration is valid
    /// var emptyTrigger = CreateTrigger();
    /// var result = manualTrigger.ValidateTrigger(emptyTrigger);
    /// // result.Success == true
    /// 
    /// // Configuration with metadata is valid
    /// var metadataTrigger = CreateTrigger(description: "Test trigger", role: "Admin");
    /// var result = manualTrigger.ValidateTrigger(metadataTrigger);
    /// // result.Success == true
    /// 
    /// // Even minimal configuration is valid
    /// var minimalTrigger = CreateMinimalTrigger();
    /// var result = manualTrigger.ValidateTrigger(minimalTrigger);
    /// // result.Success == true
    /// </code>
    /// </example>
    [ConventionOverride(MaxCyclomaticComplexity = 15)]  // Validation logic — independent type checks for optional configuration values
    public override IGenericResult ValidateTrigger(IGenericTrigger trigger)
    {
        if (trigger?.Configuration == null)
        {
            return GenericResult.Failure(SchedulingLogger.TriggerConfigurationNull(_logger));
        }

        // Validate AllowConcurrent if provided
        if (trigger.Configuration.TryGetValue(AllowConcurrentKey, out var allowConcurrentObj) &&
            allowConcurrentObj != null &&
            !TryConvertToBool(allowConcurrentObj, out var _))
        {
            return GenericResult.Failure(SchedulingLogger.ConfigurationValueMustBeBoolean(_logger, AllowConcurrentKey));
        }

        // Validate Description if provided (just check it's a string)
        if (trigger.Configuration.TryGetValue(DescriptionKey, out var descriptionObj) &&
            descriptionObj != null &&
            descriptionObj is not string)
        {
            return GenericResult.Failure(SchedulingLogger.ConfigurationValueMustBeString(_logger, DescriptionKey));
        }

        // Validate RequiredRole if provided (just check it's a string)
        if (trigger.Configuration.TryGetValue(RequiredRoleKey, out var roleObj) &&
            roleObj != null &&
            roleObj is not string)
        {
            return GenericResult.Failure(SchedulingLogger.ConfigurationValueMustBeString(_logger, RequiredRoleKey));
        }

        // Manual triggers are always valid - they have no complex requirements
        return GenericResult.Success();
    }

    /// <summary>
    /// Returns a failure result — manual triggers do not support automatic next run time calculation.
    /// </summary>
    /// <param name="trigger">Not used for manual triggers.</param>
    /// <param name="lastExecution">Not used for manual triggers.</param>
    /// <returns>
    /// Always returns a failure result, as manual triggers execute only when explicitly triggered
    /// and do not have an automatic next run time.
    /// </returns>
    public override IGenericResult<DateTimeOffset> GetNextRunTime(IGenericTrigger trigger, DateTime? lastExecution)
    {
        return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.ManualTriggerCannotComputeNextRunTime(_logger));
    }

    /// <summary>
    /// Attempts to convert an object to a boolean.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">The converted boolean value.</param>
    /// <returns><c>true</c> if conversion succeeded; otherwise, <c>false</c>.</returns>
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
    /// Helper method to assign a boolean value to the result parameter.
    /// </summary>
    /// <param name="value">The value to assign.</param>
    /// <param name="result">The result parameter to assign to.</param>
    /// <returns>Always returns <c>true</c>.</returns>
    private static bool TryAssignBool(bool value, out bool result)
    {
        result = value;
        return true;
    }
}