using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Cronos;
using Fdw.Collections.Attributes;
using Fdw.Conventions;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes.TriggerTypeImplementations;

/// <summary>
/// Window trigger type that retries repeatedly within a time window until success, then waits for the next window.
/// </summary>
/// <remarks>
/// <para>
/// The Window trigger type enables retry-within-window scheduling:
/// </para>
/// <list type="bullet">
///   <item><description>A cron expression defines when each window opens (e.g., Monday 06:00)</description></item>
///   <item><description>Within the window, execution is retried at a fixed retry interval (e.g., every 15 minutes)</description></item>
///   <item><description>The window closes after a configured duration (e.g., 4 hours)</description></item>
///   <item><description>Stop condition: first success OR window closes</description></item>
///   <item><description>Next window is calculated from the cron expression</description></item>
/// </list>
/// <para>
/// This is useful for scenarios like: "Try Monday 06:00–10:00, retrying every 15 minutes
/// until the operation succeeds, then wait until next Monday."
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Window opens Monday at 06:00, stays open 4 hours, retries every 15 minutes
/// var windowConfig = new Dictionary&lt;string, object&gt;
/// {
///     { "WindowCronExpression", "0 0 6 * * MON" },
///     { "WindowDurationMinutes", 240 },
///     { "RetryIntervalMinutes", 15 },
///     { "TimeZoneId", "America/New_York" }
/// };
///
/// var windowTrigger = TriggerTypes.Window;
/// var validationResult = windowTrigger.ValidateTrigger(trigger);
/// var nextExecution = windowTrigger.CalculateNextExecution(trigger, DateTime.UtcNow);
/// </code>
/// </example>
[TypeOption(typeof(TriggerTypes), "Window", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class Window : TriggerTypeBase
{
    private readonly ILogger<Window> _logger;

    /// <summary>
    /// Configuration key for the cron expression that defines when each window opens.
    /// </summary>
    /// <remarks>
    /// The cron expression defines the recurring window open time.
    /// Examples: "0 0 6 * * MON" (Monday 06:00), "0 0 8 * * MON-FRI" (weekdays 08:00).
    /// </remarks>
    public const string WindowCronExpressionKey = "WindowCronExpression";

    /// <summary>
    /// Configuration key for the window duration in minutes.
    /// </summary>
    /// <remarks>
    /// How long (in minutes) each window stays open. Must be a positive integer.
    /// Examples: 240 (4 hours), 60 (1 hour), 30 (30 minutes).
    /// </remarks>
    public const string WindowDurationMinutesKey = "WindowDurationMinutes";

    /// <summary>
    /// Configuration key for the retry interval in minutes within the window.
    /// </summary>
    /// <remarks>
    /// How often (in minutes) to retry within an open window. Must be a positive integer
    /// and less than the window duration to be useful.
    /// Examples: 15 (retry every 15 minutes), 5 (retry every 5 minutes).
    /// </remarks>
    public const string RetryIntervalMinutesKey = "RetryIntervalMinutes";

    /// <summary>
    /// Configuration key for the timezone identifier.
    /// </summary>
    /// <remarks>
    /// Optional timezone identifier (e.g., "America/New_York", "Europe/London", "UTC").
    /// If not provided, UTC is used. Affects when the window opens and cron calculations.
    /// </remarks>
    public const string TimeZoneIdKey = "TimeZoneId";

    /// <summary>
    /// Initializes a new instance of the <see cref="Window"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    /// <remarks>
    /// Window triggers require schedule persistence to track window state and
    /// do not execute immediately — they wait for the next window open time.
    /// </remarks>
    public Window(ILogger<Window>? logger = null) : base(5, "Window", requiresSchedule: true, isImmediate: false)
    {
        _logger = logger ?? NullLogger<Window>.Instance;
    }

    /// <summary>
    /// Calculates the next execution time based on the window configuration and last execution.
    /// </summary>
    /// <param name="trigger">The trigger configuration containing the window cron expression, duration, and retry interval.</param>
    /// <param name="lastExecution">The timestamp of the last execution attempt, or null if never executed.</param>
    /// <returns>
    /// The next execution time in UTC:
    /// <list type="bullet">
    ///   <item><description>If currently within an open window: now + retry interval</description></item>
    ///   <item><description>If window is closed or no previous execution: next window open time from cron</description></item>
    ///   <item><description>Null if configuration is invalid</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>
    /// The logic determines the current position relative to windows:
    /// </para>
    /// <list type="number">
    ///   <item><description>Parse cron expression and extract duration and retry interval</description></item>
    ///   <item><description>Find the most recent window open time before now</description></item>
    ///   <item><description>If within that window (now &lt; windowOpen + duration) and previously attempted, retry</description></item>
    ///   <item><description>Otherwise, calculate the next window open time from cron</description></item>
    /// </list>
    /// </remarks>
#pragma warning disable MA0051 // Linear window calculation with cron parsing and timezone conversion
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // Window calculation with cron parsing, timezone handling, and window boundary checks
    public override DateTime? CalculateNextExecution(IGenericTrigger trigger, DateTime? lastExecution)
    {
        if (trigger?.Configuration == null)
        {
            return null;
        }

        if (!TryGetWindowCronExpression(trigger, out var cronExpression) ||
            !TryGetWindowDurationMinutes(trigger, out var windowDurationMinutes) ||
            !TryGetRetryIntervalMinutes(trigger, out var retryIntervalMinutes))
        {
            return null;
        }

        try
        {
            var timeZone = GetTimeZone(trigger, _logger);
            var nowUtc = DateTime.UtcNow;
            var nowInZone = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone);

            var cronExpr = CronExpression.Parse(cronExpression);

            // Find the most recent window open time (occurrence before now)
            var recentWindowOpen = FindMostRecentWindowOpen(cronExpr, nowInZone, timeZone);

            if (recentWindowOpen.HasValue)
            {
                var windowOpenUtc = TimeZoneInfo.ConvertTimeToUtc(recentWindowOpen.Value, timeZone);
                var windowCloseUtc = windowOpenUtc.AddMinutes(windowDurationMinutes);

                // If we are currently inside an open window, retry after the retry interval
                if (nowUtc < windowCloseUtc && lastExecution.HasValue)
                {
                    var retryTime = lastExecution.Value.AddMinutes(retryIntervalMinutes);
                    // Don't retry past window close
                    return retryTime < windowCloseUtc ? retryTime : CalculateNextWindowOpen(cronExpr, nowInZone, timeZone);
                }
            }

            // Not in a window (or first execution) — go to the next window open time
            return CalculateNextWindowOpen(cronExpr, nowInZone, timeZone);
        }
        catch (CronFormatException ex)
        {
            // Why: an invalid cron expression at calculation time is logged and treated as
            // "no next execution" rather than propagated to the scheduler.
            SchedulingLogger.CalculateNextRunCronFormatFailed(_logger, ex, cronExpression);
            return null;
        }
        catch (TimeZoneNotFoundException ex)
        {
            // Why: unrecognised timezone during window calculation; fall back to UTC-based next open.
            SchedulingLogger.CalculateNextRunTimeZoneFailed(_logger, ex, cronExpression);
            return CalculateNextWindowOpenUtcFallback(cronExpression, _logger);
        }
        catch (ArgumentException ex)
        {
            // Why: argument/conversion errors are logged and treated as "no next execution".
            SchedulingLogger.CalculateNextRunArgumentFailed(_logger, ex, cronExpression);
            return null;
        }
    }
#pragma warning restore MA0051

    /// <summary>
    /// Returns the next run time for this window trigger.
    /// Delegates to <see cref="CalculateNextExecution"/> and wraps the result.
    /// </summary>
    /// <param name="trigger">The trigger configuration.</param>
    /// <param name="lastExecution">The last execution time, or null if never executed.</param>
    /// <returns>A success result containing the next run time, or failure if configuration is invalid.</returns>
    public override IGenericResult<DateTimeOffset> GetNextRunTime(IGenericTrigger trigger, DateTime? lastExecution)
    {
        if (trigger?.Configuration == null)
        {
            return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.TriggerConfigurationNull(_logger));
        }

        if (!TryGetWindowCronExpression(trigger, out var cronExpression))
        {
            return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.WindowCronExpressionRequired(_logger, WindowCronExpressionKey));
        }

        if (!TryGetWindowDurationMinutes(trigger, out _))
        {
            return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.WindowDurationRequired(_logger, WindowDurationMinutesKey));
        }

        if (!TryGetRetryIntervalMinutes(trigger, out _))
        {
            return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.RetryIntervalRequired(_logger, RetryIntervalMinutesKey));
        }

        var next = CalculateNextExecution(trigger, lastExecution);
        if (!next.HasValue)
        {
            return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.InvalidCronExpressionFormat(_logger, cronExpression, cronExpression));
        }

        return GenericResult<DateTimeOffset>.Success(new DateTimeOffset(next.Value, TimeSpan.Zero));
    }

    /// <summary>
    /// Validates that the trigger configuration contains a valid window cron expression,
    /// window duration, retry interval, and optional timezone.
    /// </summary>
    /// <param name="trigger">The trigger configuration to validate.</param>
    /// <returns>
    /// A success result if the trigger is valid, or an error result with validation messages if invalid.
    /// </returns>
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // Validation logic — independent checks for cron, duration, interval, and timezone
    public override IGenericResult ValidateTrigger(IGenericTrigger trigger)
    {
        if (trigger?.Configuration == null)
        {
            return GenericResult.Failure(SchedulingLogger.TriggerConfigurationNull(_logger));
        }

        // Validate window cron expression is present
        if (!trigger.Configuration.TryGetValue(WindowCronExpressionKey, out var cronExprObj) ||
            cronExprObj is not string cronExpression ||
            string.IsNullOrWhiteSpace(cronExpression))
        {
            return GenericResult.Failure(SchedulingLogger.WindowCronExpressionRequired(_logger, WindowCronExpressionKey));
        }

        // Validate cron expression format
        try
        {
            CronExpression.Parse(cronExpression);
        }
        catch (CronFormatException ex)
        {
            return GenericResult.Failure(SchedulingLogger.InvalidCronExpressionFormat(_logger, ex.Message, cronExpression));
        }
        catch (ArgumentException ex)
        {
            return GenericResult.Failure(SchedulingLogger.InvalidCronExpression(_logger, ex.Message, cronExpression));
        }

        // Validate window duration is present
        if (!trigger.Configuration.TryGetValue(WindowDurationMinutesKey, out var durationObj) ||
            !TryConvertToInt(durationObj, out var windowDurationMinutes))
        {
            return GenericResult.Failure(SchedulingLogger.WindowDurationRequired(_logger, WindowDurationMinutesKey));
        }

        if (windowDurationMinutes <= 0)
        {
            return GenericResult.Failure(SchedulingLogger.WindowDurationMustBePositive(_logger, windowDurationMinutes));
        }

        // Validate retry interval is present
        if (!trigger.Configuration.TryGetValue(RetryIntervalMinutesKey, out var retryObj) ||
            !TryConvertToInt(retryObj, out var retryIntervalMinutes))
        {
            return GenericResult.Failure(SchedulingLogger.RetryIntervalRequired(_logger, RetryIntervalMinutesKey));
        }

        if (retryIntervalMinutes <= 0)
        {
            return GenericResult.Failure(SchedulingLogger.RetryIntervalMustBePositive(_logger, retryIntervalMinutes));
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
    /// Attempts to extract and parse the window cron expression from trigger configuration.
    /// </summary>
    private static bool TryGetWindowCronExpression(IGenericTrigger trigger, out string cronExpression)
    {
        cronExpression = string.Empty;

        if (!trigger.Configuration!.TryGetValue(WindowCronExpressionKey, out var obj) ||
            obj is not string expr ||
            string.IsNullOrWhiteSpace(expr))
        {
            return false;
        }

        cronExpression = expr;
        return true;
    }

    /// <summary>
    /// Attempts to extract the window duration in minutes from trigger configuration.
    /// </summary>
    private static bool TryGetWindowDurationMinutes(IGenericTrigger trigger, out int minutes)
    {
        minutes = 0;

        if (!trigger.Configuration!.TryGetValue(WindowDurationMinutesKey, out var obj))
        {
            return false;
        }

        return TryConvertToInt(obj, out minutes) && minutes > 0;
    }

    /// <summary>
    /// Attempts to extract the retry interval in minutes from trigger configuration.
    /// </summary>
    private static bool TryGetRetryIntervalMinutes(IGenericTrigger trigger, out int minutes)
    {
        minutes = 0;

        if (!trigger.Configuration!.TryGetValue(RetryIntervalMinutesKey, out var obj))
        {
            return false;
        }

        return TryConvertToInt(obj, out minutes) && minutes > 0;
    }

    /// <summary>
    /// Gets the configured timezone or UTC if not specified or invalid.
    /// </summary>
    private static TimeZoneInfo GetTimeZone(IGenericTrigger trigger, ILogger logger)
    {
        if (trigger.Configuration!.TryGetValue(TimeZoneIdKey, out var obj) &&
            obj is string timeZoneId &&
            !string.IsNullOrWhiteSpace(timeZoneId))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException ex)
            {
                // Why: timezone not found at calculation time; log and fall through to UTC so
                // the calculation can still produce a result.
                SchedulingLogger.TimezoneConversionSkippedInValidation(logger, ex);
            }
        }

        return TimeZoneInfo.Utc;
    }

    /// <summary>
    /// Finds the most recent window open occurrence before now, within the last window duration.
    /// </summary>
    private static DateTime? FindMostRecentWindowOpen(CronExpression cronExpr, DateTime nowInZone, TimeZoneInfo timeZone)
    {
        // Look back up to 31 days to find the most recent occurrence
        var lookbackStart = nowInZone.AddDays(-31);
        var previous = cronExpr.GetNextOccurrence(lookbackStart, timeZone);

        DateTime? mostRecent = null;
        while (previous.HasValue && previous.Value <= nowInZone)
        {
            mostRecent = previous.Value;
            previous = cronExpr.GetNextOccurrence(previous.Value, timeZone);
        }

        return mostRecent;
    }

    /// <summary>
    /// Calculates the next window open time after now using the cron expression.
    /// </summary>
    private static DateTime? CalculateNextWindowOpen(CronExpression cronExpr, DateTime nowInZone, TimeZoneInfo timeZone)
    {
        var next = cronExpr.GetNextOccurrence(nowInZone, timeZone);
        return next?.ToUniversalTime();
    }

    /// <summary>
    /// Fallback for when timezone resolution fails: calculates next window open in UTC.
    /// </summary>
    private static DateTime? CalculateNextWindowOpenUtcFallback(string cronExpression, ILogger logger)
    {
        try
        {
            var cronExpr = CronExpression.Parse(cronExpression);
            return cronExpr.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc);
        }
        catch (Exception ex)
        {
            // Why: log so the exception is observed; return null so the scheduler treats this
            // trigger as having no calculable next execution time.
            SchedulingLogger.CalculateNextRunFallbackFailed(logger, ex);
            return null;
        }
    }

    /// <summary>
    /// Attempts to convert an object to an integer.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">The converted integer value.</param>
    /// <returns><c>true</c> if conversion succeeded; otherwise, <c>false</c>.</returns>
    private static bool TryConvertToInt(object? value, out int result)
    {
        result = 0;

        return value switch
        {
            int intValue => TryAssignInt(intValue, out result),
            long longValue when longValue >= int.MinValue && longValue <= int.MaxValue => TryAssignInt((int)longValue, out result),
            decimal decimalValue when decimalValue == Math.Truncate(decimalValue) && decimalValue >= int.MinValue && decimalValue <= int.MaxValue => TryAssignInt((int)decimalValue, out result),
            double doubleValue when doubleValue == Math.Truncate(doubleValue) && doubleValue >= int.MinValue && doubleValue <= int.MaxValue => TryAssignInt((int)doubleValue, out result),
            string stringValue => int.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out result),
            _ => false
        };
    }

    /// <summary>
    /// Helper method to assign an integer value to the result parameter.
    /// </summary>
    private static bool TryAssignInt(int value, out int result)
    {
        result = value;
        return true;
    }
}
