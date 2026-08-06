using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Conventions;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes.TriggerTypeImplementations;

/// <summary>
/// Once trigger type that executes at a specific date and time only once.
/// </summary>
/// <remarks>
/// <para>
/// The Once trigger type enables one-time execution at a specified date and time
/// with optional timezone support. It supports:
/// </para>
/// <list type="bullet">
///   <item><description>Specific execution time specification</description></item>
///   <item><description>Timezone-aware scheduling for accurate execution</description></item>
///   <item><description>Automatic handling of daylight saving time</description></item>
///   <item><description>One-time execution guarantee (never executes twice)</description></item>
/// </list>
/// <para>
/// The trigger calculates the next execution as the specified start time only if
/// it has never executed before. Once executed, it returns null for all future
/// next execution calculations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get a once trigger for a specific time
/// var onceConfig = new Dictionary&lt;string, object&gt;
/// {
///     { "StartTime", new DateTime(2024, 12, 31, 23, 59, 0) }
/// };
/// 
/// // With timezone specification
/// var onceConfig = new Dictionary&lt;string, object&gt;
/// {
///     { "StartTime", new DateTime(2024, 1, 1, 9, 0, 0) },
///     { "TimeZoneId", "America/New_York" }
/// };
/// 
/// // Validate and calculate next execution
/// var onceTrigger = TriggerTypes.Once;
/// var validationResult = onceTrigger.ValidateTrigger(trigger);
/// var nextExecution = onceTrigger.CalculateNextExecution(trigger, null);
/// </code>
/// </example>
[TypeOption(typeof(TriggerTypes), "Once", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class Once : TriggerTypeBase
{
    private readonly ILogger<Once> _logger;
    /// <summary>
    /// Configuration key for the start time when the trigger should execute.
    /// </summary>
    /// <remarks>
    /// The start time specifies when the one-time execution should occur.
    /// Should be a DateTime value. If not provided, the trigger executes immediately when created.
    /// </remarks>
    public const string StartTimeKey = "StartTime";

    /// <summary>
    /// Configuration key for the timezone identifier.
    /// </summary>
    /// <remarks>
    /// Optional timezone identifier (e.g., "America/New_York", "Europe/London", "UTC").
    /// If not provided, UTC is used. The timezone affects when the start time is interpreted
    /// and handles daylight saving time automatically.
    /// </remarks>
    public const string TimeZoneIdKey = "TimeZoneId";

    /// <summary>
    /// Initializes a new instance of the <see cref="Once"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    /// <remarks>
    /// Once triggers do not require schedule persistence as they execute only once and
    /// do not execute immediately - they wait for their specified execution time.
    /// </remarks>
    public Once(ILogger<Once>? logger = null) : base(3, "Once", requiresSchedule: false, isImmediate: false)
    {
        _logger = logger ?? NullLogger<Once>.Instance;
    }

    /// <summary>
    /// Calculates the next execution time based on the start time and execution history.
    /// </summary>
    /// <param name="trigger">The trigger configuration containing the start time.</param>
    /// <param name="lastExecution">The timestamp of the last execution, or null if never executed.</param>
    /// <returns>
    /// The start time in UTC if never executed before, or null if already executed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method calculates the next execution time using the following logic:
    /// </para>
    /// <list type="number">
    ///   <item><description>If lastExecution is not null, return null (already executed)</description></item>
    ///   <item><description>Extract start time from configuration or use current time</description></item>
    ///   <item><description>Handle timezone conversion if specified</description></item>
    ///   <item><description>Return start time in UTC, or null if time has already passed</description></item>
    /// </list>
    /// <para>
    /// The method ensures one-time execution by returning null once the trigger
    /// has been executed previously.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // First execution calculation (never executed before)
    /// var trigger = CreateTrigger(startTime: futureDateTime);
    /// var nextExecution = onceTrigger.CalculateNextExecution(trigger, null);
    /// // Returns futureDateTime in UTC
    /// 
    /// // After execution (already executed)
    /// var trigger = CreateTrigger(startTime: futureDateTime);
    /// var nextExecution = onceTrigger.CalculateNextExecution(trigger, pastDateTime);
    /// // Returns null - trigger only executes once
    /// 
    /// // No start time specified
    /// var trigger = CreateTrigger();
    /// var nextExecution = onceTrigger.CalculateNextExecution(trigger, null);
    /// // Returns current time (executes immediately)
    /// </code>
    /// </example>
#pragma warning disable MA0051 // Linear one-time execution calculation with timezone handling
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // One-time execution calculation with timezone conversion and multiple fallback paths
    public override DateTime? CalculateNextExecution(IGenericTrigger trigger, DateTime? lastExecution)
    {
        if (trigger?.Configuration == null)
        {
            return null;
        }

        // If already executed, never execute again
        if (lastExecution.HasValue)
        {
            return null;
        }

        try
        {
            // Get timezone, default to UTC
            TimeZoneInfo timeZone = TimeZoneInfo.Utc;
            if (trigger.Configuration.TryGetValue(TimeZoneIdKey, out var timeZoneObj) &&
                timeZoneObj is string timeZoneId &&
                !string.IsNullOrWhiteSpace(timeZoneId))
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }

            // Get start time or use current time
            DateTime startTime;
            if (trigger.Configuration.TryGetValue(StartTimeKey, out var startTimeObj) &&
                TryConvertToDateTime(startTimeObj, out var configuredStartTime))
            {
                // Convert configured time to UTC if needed
                startTime = configuredStartTime.Kind == DateTimeKind.Utc
                    ? configuredStartTime
                    : TimeZoneInfo.ConvertTimeToUtc(configuredStartTime, timeZone);
            }
            else
            {
                // No start time specified - execute immediately
                startTime = DateTime.UtcNow;
            }

            // Only return the start time if it's in the future
            return startTime > DateTime.UtcNow ? startTime : DateTime.UtcNow;
        }
        catch (TimeZoneNotFoundException ex)
        {
            // Why: timezone is invalid; log the failure and fall back to a UTC-based calculation.
            SchedulingLogger.CalculateNextRunTimeZoneError(_logger, ex);
            try
            {
                if (trigger.Configuration.TryGetValue(StartTimeKey, out var startTimeObj) &&
                    TryConvertToDateTime(startTimeObj, out var startTime))
                {
                    var utcStartTime = startTime.Kind == DateTimeKind.Utc ? startTime : startTime.ToUniversalTime();
                    return utcStartTime > DateTime.UtcNow ? utcStartTime : DateTime.UtcNow;
                }
                else
                {
                    return DateTime.UtcNow;
                }
            }
            catch (Exception innerEx)
            {
                // Why: log the inner exception so it is observed; return null so the scheduler
                // treats this trigger as having no calculable next execution time.
                SchedulingLogger.CalculateNextRunFallbackFailed(_logger, innerEx);
                return null;
            }
        }
        catch (ArgumentException ex)
        {
            // Why: argument/conversion errors during calculation are logged and treated as
            // "no next execution" rather than propagated.
            SchedulingLogger.CalculateNextRunArgumentError(_logger, ex);
            return null;
        }
    }
#pragma warning restore MA0051

    /// <summary>
    /// Validates that the trigger configuration contains valid parameters.
    /// </summary>
    /// <param name="trigger">The trigger configuration to validate.</param>
    /// <returns>
    /// A success result if the trigger is valid, or an error result with validation messages if invalid.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs comprehensive validation of the once trigger configuration:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><strong>Start time validation</strong>: Verifies start time format if provided</description></item>
    ///   <item><description><strong>Timezone validation</strong>: Verifies timezone ID exists if provided</description></item>
    ///   <item><description><strong>Future time validation</strong>: Warns if start time is in the past</description></item>
    /// </list>
    /// <para>
    /// The validation is lenient for once triggers since they may be created for immediate
    /// execution or future execution, and past times are converted to immediate execution.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Valid once trigger with future time
    /// var validTrigger = CreateTrigger(startTime: DateTime.UtcNow.AddHours(1));
    /// var result = onceTrigger.ValidateTrigger(validTrigger);
    /// // result.Success == true
    /// 
    /// // Valid trigger without start time (immediate execution)
    /// var immediateTrigger = CreateTrigger();
    /// var result = onceTrigger.ValidateTrigger(immediateTrigger);
    /// // result.Success == true
    /// 
    /// // Invalid timezone
    /// var badTimezoneTrigger = CreateTrigger(startTime: futureTime, timezone: "Invalid/Timezone");
    /// var result = onceTrigger.ValidateTrigger(badTimezoneTrigger);
    /// // result.Error == true with timezone validation error
    /// </code>
    /// </example>
#pragma warning disable MA0051 // Linear trigger validation: check execution time, timezone, expiry
    [ConventionOverride(MaxCyclomaticComplexity = 25)]  // Validation logic — start time validation with timezone conversion and multiple checks
    public override IGenericResult ValidateTrigger(IGenericTrigger trigger)
    {
        if (trigger?.Configuration == null)
        {
            return GenericResult.Failure(SchedulingLogger.TriggerConfigurationNull(_logger));
        }

        // Validate start time if provided
        if (trigger.Configuration.TryGetValue(StartTimeKey, out var startTimeObj) &&
            startTimeObj != null &&
            !TryConvertToDateTime(startTimeObj, out var startTime))
        {
            return GenericResult.Failure(SchedulingLogger.InvalidStartTime(_logger, StartTimeKey));
        }

        // Validate timezone if provided
        if (trigger.Configuration.TryGetValue(TimeZoneIdKey, out var timeZoneObj) &&
            timeZoneObj is string timeZoneId &&
            !string.IsNullOrWhiteSpace(timeZoneId))
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException ex)
            {
                return GenericResult.Failure(SchedulingLogger.InvalidTimezoneIdentifierWithException(_logger, ex, timeZoneId, ex.Message));
            }
            catch (InvalidTimeZoneException ex)
            {
                return GenericResult.Failure(SchedulingLogger.InvalidTimezoneConfiguration(_logger, ex.Message, timeZoneId));
            }
        }

        // Additional validation: Check if start time is reasonable if provided
        if (trigger.Configuration.TryGetValue(StartTimeKey, out var startTimeCheck) &&
            TryConvertToDateTime(startTimeCheck, out var checkStartTime))
        {
            // Get timezone for accurate comparison
            TimeZoneInfo timeZone = TimeZoneInfo.Utc;
            if (trigger.Configuration.TryGetValue(TimeZoneIdKey, out var tzObj) &&
                tzObj is string tzId &&
                !string.IsNullOrWhiteSpace(tzId))
            {
                timeZone = ResolveTimeZoneOrUtc(tzId);
            }

            // Check if start time is too far in the past (more than 1 day)
            if (TryConvertToUtc(checkStartTime, timeZone, out var utcStartTime) &&
                utcStartTime < DateTime.UtcNow.AddDays(-1))
            {
                return GenericResult.Failure(SchedulingLogger.StartTimeTooFarInPast(_logger, utcStartTime.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)));
            }
        }

        return GenericResult.Success();
    }
#pragma warning restore MA0051

    // Why: FindSystemTimeZoneById throws only TimeZoneNotFoundException/InvalidTimeZoneException for a
    // bad id (already validated above); extracting keeps the UTC fallback out of the result-returning
    // ValidateTrigger, and the narrow filter lets any other (unexpected) exception fail loud.
    private TimeZoneInfo ResolveTimeZoneOrUtc(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            SchedulingLogger.TimezoneConversionSkippedInValidation(_logger, ex);
            return TimeZoneInfo.Utc;
        }
    }

    // Why: ConvertTimeToUtc throws only ArgumentException/InvalidTimeZoneException for kind/zone
    // mismatches; extracting keeps the best-effort swallow out of the result-returning ValidateTrigger,
    // and the narrow filter lets any other (unexpected) exception fail loud.
    private bool TryConvertToUtc(DateTime startTime, TimeZoneInfo timeZone, out DateTime utcStartTime)
    {
        try
        {
            utcStartTime = startTime.Kind == DateTimeKind.Utc
                ? startTime
                : TimeZoneInfo.ConvertTimeToUtc(startTime, timeZone);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidTimeZoneException)
        {
            SchedulingLogger.TimezoneConversionSkippedInValidation(_logger, ex);
            utcStartTime = default;
            return false;
        }
    }

    /// <summary>
    /// Calculates the next run time for this once trigger, returning a structured result.
    /// </summary>
    /// <param name="trigger">The trigger configuration containing the start time.</param>
    /// <param name="lastExecution">The timestamp of the last execution, or null if never executed.</param>
    /// <returns>
    /// A success result containing the configured execution time in UTC if never executed before,
    /// or a failure result if the trigger has already executed or configuration is invalid.
    /// </returns>
#pragma warning disable MA0051 // Linear one-time execution calculation with timezone handling
    [ConventionOverride(MaxCyclomaticComplexity = 20)]
    public override IGenericResult<DateTimeOffset> GetNextRunTime(IGenericTrigger trigger, DateTime? lastExecution)
    {
        if (trigger?.Configuration == null)
        {
            return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.TriggerConfigurationNull(_logger));
        }

        if (lastExecution.HasValue)
        {
            return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.StartTimeTooFarInPast(_logger,
                lastExecution.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)));
        }

        try
        {
            TimeZoneInfo timeZone = TimeZoneInfo.Utc;
            if (trigger.Configuration.TryGetValue(TimeZoneIdKey, out var timeZoneObj) &&
                timeZoneObj is string timeZoneId &&
                !string.IsNullOrWhiteSpace(timeZoneId))
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }

            DateTime startTime;
            if (trigger.Configuration.TryGetValue(StartTimeKey, out var startTimeObj) &&
                TryConvertToDateTime(startTimeObj, out var configuredStartTime))
            {
                startTime = configuredStartTime.Kind == DateTimeKind.Utc
                    ? configuredStartTime
                    : TimeZoneInfo.ConvertTimeToUtc(configuredStartTime, timeZone);
            }
            else
            {
                startTime = DateTime.UtcNow;
            }

            var nextUtc = startTime > DateTime.UtcNow ? startTime : DateTime.UtcNow;
            return GenericResult<DateTimeOffset>.Success(new DateTimeOffset(nextUtc, TimeSpan.Zero));
        }
        catch (TimeZoneNotFoundException ex)
        {
            return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.InvalidTimezoneIdentifier(_logger, ex.Message));
        }
        catch (ArgumentException ex)
        {
            return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.InvalidStartTime(_logger, ex.Message));
        }
    }
#pragma warning restore MA0051

    /// <summary>
    /// Attempts to convert an object to a DateTime.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">The converted DateTime value.</param>
    /// <returns><c>true</c> if conversion succeeded; otherwise, <c>false</c>.</returns>
    private static bool TryConvertToDateTime(object? value, out DateTime result)
    {
        result = default;

        return value switch
        {
            DateTime dateTimeValue => TryAssignDateTime(dateTimeValue, out result),
            DateTimeOffset dateTimeOffsetValue => TryAssignDateTime(dateTimeOffsetValue.DateTime, out result),
            string stringValue => DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out result),
            _ => false
        };
    }

    /// <summary>
    /// Helper method to assign a DateTime value to the result parameter.
    /// </summary>
    /// <param name="value">The value to assign.</param>
    /// <param name="result">The result parameter to assign to.</param>
    /// <returns>Always returns <c>true</c>.</returns>
    private static bool TryAssignDateTime(DateTime value, out DateTime result)
    {
        result = value;
        return true;
    }
}