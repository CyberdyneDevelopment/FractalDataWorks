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
/// Interval trigger type that executes at regular intervals.
/// </summary>
/// <remarks>
/// <para>
/// The Interval trigger type enables regular, recurring execution at specified intervals
/// with optional start time and timezone support. It supports:
/// </para>
/// <list type="bullet">
///   <item><description>Fixed intervals in minutes, hours, or days</description></item>
///   <item><description>Optional start time specification</description></item>
///   <item><description>Timezone-aware scheduling for consistent intervals</description></item>
///   <item><description>Automatic handling of daylight saving time transitions</description></item>
/// </list>
/// <para>
/// The trigger calculates the next execution by adding the specified interval to the
/// last execution time (or start time if never executed before).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get an interval trigger for every 30 minutes
/// var intervalConfig = new Dictionary&lt;string, object&gt;
/// {
///     { "IntervalMinutes", 30 }
/// };
/// 
/// // With start time and timezone
/// var intervalConfig = new Dictionary&lt;string, object&gt;
/// {
///     { "IntervalMinutes", 60 },
///     { "StartTime", DateTime.Today.AddHours(9) },
///     { "TimeZoneId", "America/New_York" }
/// };
/// 
/// // Validate and calculate next execution
/// var intervalTrigger = TriggerTypes.Interval;
/// var validationResult = intervalTrigger.ValidateTrigger(trigger);
/// var nextExecution = intervalTrigger.CalculateNextExecution(trigger, DateTime.UtcNow);
/// </code>
/// </example>
[TypeOption(typeof(TriggerTypes), "Interval", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class Interval : TriggerTypeBase
{
    private readonly ILogger<Interval> _logger;
    /// <summary>
    /// Configuration key for the interval in minutes.
    /// </summary>
    /// <remarks>
    /// The interval must be a positive integer representing the number of minutes
    /// between executions. Examples: 15 (every 15 minutes), 60 (hourly), 1440 (daily).
    /// </remarks>
    public const string IntervalMinutesKey = "IntervalMinutes";

    /// <summary>
    /// Configuration key for the optional start time.
    /// </summary>
    /// <remarks>
    /// Optional start time for the first execution. If not provided, the first execution
    /// will be one interval after the trigger is created. Should be a DateTime value.
    /// </remarks>
    public const string StartTimeKey = "StartTime";

    /// <summary>
    /// Configuration key for the timezone identifier.
    /// </summary>
    /// <remarks>
    /// Optional timezone identifier (e.g., "America/New_York", "Europe/London", "UTC").
    /// If not provided, UTC is used. The timezone affects when intervals are calculated
    /// and handles daylight saving time transitions automatically.
    /// </remarks>
    public const string TimeZoneIdKey = "TimeZoneId";

    /// <summary>
    /// Initializes a new instance of the <see cref="Interval"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    /// <remarks>
    /// Interval triggers require schedule persistence to track next execution times and
    /// do not execute immediately - they wait for their calculated schedule time.
    /// </remarks>
    public Interval(ILogger<Interval>? logger = null) : base(2, "Interval", requiresSchedule: true, isImmediate: false)
    {
        _logger = logger ?? NullLogger<Interval>.Instance;
    }

    /// <summary>
    /// Calculates the next execution time based on the interval and last execution.
    /// </summary>
    /// <param name="trigger">The trigger configuration containing the interval and optional start time.</param>
    /// <param name="lastExecution">The timestamp of the last execution, or null if never executed.</param>
    /// <returns>
    /// The next execution time in UTC, calculated as (lastExecution ?? startTime ?? now) + interval.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method calculates the next execution time using the following logic:
    /// </para>
    /// <list type="number">
    ///   <item><description>Extract interval in minutes from trigger configuration</description></item>
    ///   <item><description>Determine reference time: lastExecution, startTime, or current time</description></item>
    ///   <item><description>Add interval to reference time, handling timezone if specified</description></item>
    ///   <item><description>Convert result back to UTC for consistent storage</description></item>
    /// </list>
    /// <para>
    /// The method handles timezone conversions and daylight saving time transitions
    /// by using the configured timezone for interval calculations.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Every 30 minutes from now
    /// var trigger = CreateTrigger(intervalMinutes: 30);
    /// var nextExecution = intervalTrigger.CalculateNextExecution(trigger, null);
    /// // Returns current time + 30 minutes
    /// 
    /// // Every hour from last execution
    /// var trigger = CreateTrigger(intervalMinutes: 60);
    /// var nextExecution = intervalTrigger.CalculateNextExecution(trigger, lastRun);
    /// // Returns lastRun + 60 minutes
    /// 
    /// // Every 2 hours starting at 9 AM Eastern
    /// var trigger = CreateTrigger(intervalMinutes: 120, startTime: today9AM, timezone: "America/New_York");
    /// var nextExecution = intervalTrigger.CalculateNextExecution(trigger, null);
    /// // Returns 9 AM Eastern + 2 hours (first execution after start time)
    /// </code>
    /// </example>
#pragma warning disable MA0051 // Linear interval calculation with timezone conversion and fallback
    [ConventionOverride(MaxCyclomaticComplexity = 25)]  // Interval calculation with timezone conversion and multiple fallback paths
    public override DateTime? CalculateNextExecution(IGenericTrigger trigger, DateTime? lastExecution)
    {
        if (trigger?.Configuration == null)
        {
            return null;
        }

        // Extract interval in minutes
        if (!trigger.Configuration.TryGetValue(IntervalMinutesKey, out var intervalObj) ||
            !TryConvertToInt(intervalObj, out var intervalMinutes) ||
            intervalMinutes <= 0)
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

            // Determine reference time: lastExecution, startTime, or current time
            DateTime referenceTime;
            if (lastExecution.HasValue)
            {
                referenceTime = lastExecution.Value;
            }
            else if (trigger.Configuration.TryGetValue(StartTimeKey, out var startTimeObj) &&
                     TryConvertToDateTime(startTimeObj, out var startTime))
            {
                referenceTime = startTime.Kind == DateTimeKind.Utc ? startTime : startTime.ToUniversalTime();
            }
            else
            {
                referenceTime = DateTime.UtcNow;
            }

            // Convert reference time to target timezone for calculation
            var referenceTimeInZone = TimeZoneInfo.ConvertTimeFromUtc(referenceTime, timeZone);

            // Add interval
            var nextExecutionInZone = referenceTimeInZone.AddMinutes(intervalMinutes);

            // Convert back to UTC for storage
            return TimeZoneInfo.ConvertTimeToUtc(nextExecutionInZone, timeZone);
        }
        catch (TimeZoneNotFoundException ex)
        {
            // Invalid timezone — log warning and fall back to UTC calculation
            SchedulingLogger.CalculateNextRunTimeZoneFailed(_logger, ex, "Interval");
            try
            {
                DateTime referenceTime;
                if (lastExecution.HasValue)
                {
                    referenceTime = lastExecution.Value;
                }
                else if (trigger.Configuration.TryGetValue(StartTimeKey, out var startTimeObj) &&
                         TryConvertToDateTime(startTimeObj, out var startTime))
                {
                    referenceTime = startTime.Kind == DateTimeKind.Utc ? startTime : startTime.ToUniversalTime();
                }
                else
                {
                    referenceTime = DateTime.UtcNow;
                }

                return referenceTime.AddMinutes(intervalMinutes);
            }
            catch (Exception fallbackEx)
            {
                // UTC fallback also failed — log and give up
                SchedulingLogger.CalculateNextRunFallbackFailed(_logger, fallbackEx);
                return null;
            }
        }
        catch (ArgumentException ex)
        {
            // Other timezone or datetime conversion errors — log and return null
            SchedulingLogger.CalculateNextRunArgumentFailed(_logger, ex, IntervalMinutesKey);
            return null;
        }
    }
#pragma warning restore MA0051

    /// <summary>
    /// Determines whether the interval trigger is due, treating a never-executed trigger as due
    /// immediately.
    /// </summary>
    /// <param name="trigger">The trigger to evaluate.</param>
    /// <param name="lastExecution">The last execution time, or <see langword="null"/> if never executed.</param>
    /// <param name="now">The current evaluation time.</param>
    /// <returns><c>true</c> when due; <c>false</c> otherwise.</returns>
    /// <remarks>
    /// Why: the base <see cref="TriggerTypeBase.IsDue"/> delegates to
    /// <see cref="CalculateNextExecution"/>, which for a null <paramref name="lastExecution"/> returns
    /// <c>now + interval</c> — a stateless evaluator (SchedulerBackgroundService polls without
    /// persisting the computed next time) recomputes that on every pass, so a freshly created
    /// schedule (null <c>LastRunTime</c>) would NEVER become due (FDW-576). First evaluation
    /// dispatches immediately; the recorded <c>LastRunTime</c> then anchors the exact interval
    /// cadence via the base behavior. A trigger with no valid interval configuration stays
    /// not-due (fail-loud at validation, never a guessed dispatch) — same pattern as
    /// <see cref="Cron"/>'s own <c>IsDue</c> override.
    /// </remarks>
    public override bool IsDue(IGenericTrigger trigger, DateTime? lastExecution, DateTimeOffset now)
    {
        if (lastExecution.HasValue)
        {
            return base.IsDue(trigger, lastExecution, now);
        }

        return trigger?.Configuration != null
            && trigger.Configuration.TryGetValue(IntervalMinutesKey, out var intervalObj)
            && TryConvertToInt(intervalObj, out var intervalMinutes)
            && intervalMinutes > 0;
    }

    /// <summary>
    /// Validates that the trigger configuration contains a valid interval and optional parameters.
    /// </summary>
    /// <param name="trigger">The trigger configuration to validate.</param>
    /// <returns>
    /// A success result if the trigger is valid, or an error result with validation messages if invalid.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs comprehensive validation of the interval trigger configuration:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><strong>Interval validation</strong>: Ensures interval is positive integer</description></item>
    ///   <item><description><strong>Start time validation</strong>: Verifies start time format if provided</description></item>
    ///   <item><description><strong>Timezone validation</strong>: Verifies timezone ID exists if provided</description></item>
    ///   <item><description><strong>Configuration completeness</strong>: Ensures required parameters are present</description></item>
    /// </list>
    /// <para>
    /// The validation ensures the interval trigger will work correctly during execution
    /// and provides detailed error messages for any configuration issues.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Valid interval trigger
    /// var validTrigger = CreateTrigger(intervalMinutes: 30, timezone: "UTC");
    /// var result = intervalTrigger.ValidateTrigger(validTrigger);
    /// // result.Success == true
    /// 
    /// // Invalid interval (negative)
    /// var invalidTrigger = CreateTrigger(intervalMinutes: -10);
    /// var result = intervalTrigger.ValidateTrigger(invalidTrigger);
    /// // result.Error == true, result.Messages contains validation errors
    /// 
    /// // Invalid timezone
    /// var badTimezoneTrigger = CreateTrigger(intervalMinutes: 60, timezone: "Invalid/Timezone");
    /// var result = intervalTrigger.ValidateTrigger(badTimezoneTrigger);
    /// // result.Error == true with timezone validation error
    /// </code>
    /// </example>
    [ConventionOverride(MaxCyclomaticComplexity = 15)]  // Validation logic — independent checks for interval, start time, and timezone
    public override IGenericResult ValidateTrigger(IGenericTrigger trigger)
    {
        if (trigger?.Configuration == null)
        {
            return GenericResult.Failure(SchedulingLogger.TriggerConfigurationNull(_logger));
        }

        // Validate interval is present and positive
        if (!trigger.Configuration.TryGetValue(IntervalMinutesKey, out var intervalObj) ||
            !TryConvertToInt(intervalObj, out var intervalMinutes))
        {
            return GenericResult.Failure(SchedulingLogger.IntervalRequired(_logger, IntervalMinutesKey));
        }

        if (intervalMinutes <= 0)
        {
            return GenericResult.Failure(SchedulingLogger.IntervalMustBePositive(_logger, intervalMinutes));
        }

        // Validate start time if provided
        if (trigger.Configuration.TryGetValue(StartTimeKey, out var startTimeObj) &&
            startTimeObj != null &&
            !TryConvertToDateTime(startTimeObj, out var _))
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

        return GenericResult.Success();
    }

    /// <summary>
    /// Calculates the next run time for this interval trigger, returning a structured result.
    /// </summary>
    /// <param name="trigger">The trigger configuration containing the interval and optional start time.</param>
    /// <param name="lastExecution">The timestamp of the last execution, or null if never executed.</param>
    /// <returns>
    /// A success result containing the next execution time in UTC as (lastExecution ?? startTime ?? now) + interval,
    /// or a failure result if the interval configuration is invalid.
    /// </returns>
#pragma warning disable MA0051 // Linear interval calculation with timezone conversion and fallback
    [ConventionOverride(MaxCyclomaticComplexity = 25)]
    public override IGenericResult<DateTimeOffset> GetNextRunTime(IGenericTrigger trigger, DateTime? lastExecution)
    {
        if (trigger?.Configuration == null)
        {
            return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.TriggerConfigurationNull(_logger));
        }

        if (!trigger.Configuration.TryGetValue(IntervalMinutesKey, out var intervalObj) ||
            !TryConvertToInt(intervalObj, out var intervalMinutes))
        {
            return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.IntervalRequired(_logger, IntervalMinutesKey));
        }

        if (intervalMinutes <= 0)
        {
            return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.IntervalMustBePositive(_logger, intervalMinutes));
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

            DateTime referenceTime;
            if (lastExecution.HasValue)
            {
                referenceTime = lastExecution.Value;
            }
            else if (trigger.Configuration.TryGetValue(StartTimeKey, out var startTimeObj) &&
                     TryConvertToDateTime(startTimeObj, out var startTime))
            {
                referenceTime = startTime.Kind == DateTimeKind.Utc ? startTime : startTime.ToUniversalTime();
            }
            else
            {
                referenceTime = DateTime.UtcNow;
            }

            var referenceTimeInZone = TimeZoneInfo.ConvertTimeFromUtc(referenceTime, timeZone);
            var nextExecutionInZone = referenceTimeInZone.AddMinutes(intervalMinutes);
            var nextUtc = TimeZoneInfo.ConvertTimeToUtc(nextExecutionInZone, timeZone);

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
    /// Attempts to convert an object to an integer.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">The converted integer value.</param>
    /// <returns><c>true</c> if conversion succeeded; otherwise, <c>false</c>.</returns>
    [ConventionOverride(MaxCyclomaticComplexity = 15)]  // Type conversion logic — multiple numeric type conversions
    private static bool TryConvertToInt(object? value, out int result)
    {
        result = 0;

        return value switch
        {
            int intValue => TryAssignInt(intValue, out result),
            long longValue when longValue >= int.MinValue && longValue <= int.MaxValue => TryAssignInt((int)longValue, out result),
            decimal decimalValue when decimalValue == Math.Truncate(decimalValue) && decimalValue >= int.MinValue && decimalValue <= int.MaxValue => TryAssignInt((int)decimalValue, out result),
            double doubleValue when doubleValue == Math.Truncate(doubleValue) && doubleValue >= int.MinValue && doubleValue <= int.MaxValue => TryAssignInt((int)doubleValue, out result),
            float floatValue when floatValue == Math.Truncate(floatValue) && floatValue >= int.MinValue && floatValue <= int.MaxValue => TryAssignInt((int)floatValue, out result),
            string stringValue => int.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out result),
            _ => false
        };
    }

    /// <summary>
    /// Helper method to assign an integer value to the result parameter.
    /// </summary>
    /// <param name="value">The value to assign.</param>
    /// <param name="result">The result parameter to assign to.</param>
    /// <returns>Always returns <c>true</c>.</returns>
    private static bool TryAssignInt(int value, out int result)
    {
        result = value;
        return true;
    }

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