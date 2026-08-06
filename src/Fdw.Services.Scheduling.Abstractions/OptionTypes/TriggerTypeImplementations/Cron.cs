using System;
using System.Diagnostics.CodeAnalysis;
using Cronos;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Conventions;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes.TriggerTypeImplementations;

/// <summary>
/// Cron trigger type that uses cron expressions for time-based scheduling.
/// </summary>
/// <remarks>
/// <para>
/// The Cron trigger type enables complex time-based scheduling using standard cron expressions
/// with optional timezone support. It supports all standard cron formats including:
/// </para>
/// <list type="bullet">
///   <item><description>Second-precision cron expressions (6 fields)</description></item>
///   <item><description>Minute-precision cron expressions (5 fields)</description></item>
///   <item><description>Extended cron syntax with descriptors like @yearly, @monthly, @daily, @hourly</description></item>
/// </list>
/// <para>
/// The trigger validates cron expressions using the Cronos library and handles timezone
/// conversions for accurate scheduling across different time zones.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get a cron trigger for daily execution at 9 AM EST
/// var cronConfig = new Dictionary&lt;string, object&gt;
/// {
///     { "CronExpression", "0 9 * * *" },
///     { "TimeZoneId", "America/New_York" }
/// };
/// 
/// // Validate and calculate next execution
/// var cronTrigger = TriggerTypes.Cron;
/// var validationResult = cronTrigger.ValidateTrigger(trigger);
/// var nextExecution = cronTrigger.CalculateNextExecution(trigger, DateTime.UtcNow);
/// </code>
/// </example>
[TypeOption(typeof(TriggerTypes), "Cron", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class Cron : TriggerTypeBase
{
    private readonly ILogger<Cron> _logger;
    /// <summary>
    /// Configuration key for the cron expression.
    /// </summary>
    /// <remarks>
    /// The cron expression should be a valid cron format supported by the Cronos library.
    /// Examples: "0 9 * * MON-FRI", "0 0 1 * *", "@daily", "0 */15 * * * *"
    /// </remarks>
    public const string CronExpressionKey = "CronExpression";

    /// <summary>
    /// Configuration key for the timezone identifier.
    /// </summary>
    /// <remarks>
    /// Optional timezone identifier (e.g., "America/New_York", "Europe/London", "UTC").
    /// If not provided, UTC is used. The timezone affects when the cron expression executes
    /// and handles daylight saving time transitions automatically.
    /// </remarks>
    public const string TimeZoneIdKey = "TimeZoneId";

    /// <summary>
    /// Initializes a new instance of the <see cref="Cron"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    /// <remarks>
    /// Cron triggers require schedule persistence to track next execution times and
    /// do not execute immediately - they wait for their calculated schedule time.
    /// </remarks>
    public Cron(ILogger<Cron>? logger = null) : base(1, "Cron", requiresSchedule: true, isImmediate: false)
    {
        _logger = logger ?? NullLogger<Cron>.Instance;
    }

    /// <summary>
    /// Calculates the next execution time based on the cron expression and timezone.
    /// </summary>
    /// <param name="trigger">The trigger configuration containing the cron expression and optional timezone.</param>
    /// <param name="lastExecution">The timestamp of the last execution, or null if never executed.</param>
    /// <returns>
    /// The next execution time in UTC, or null if the cron expression will never match again.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method parses the cron expression from the trigger configuration and calculates
    /// the next occurrence after the specified reference time. The calculation process:
    /// </para>
    /// <list type="number">
    ///   <item><description>Extract cron expression from trigger configuration</description></item>
    ///   <item><description>Parse timezone if provided, default to UTC</description></item>
    ///   <item><description>Use the later of lastExecution or current time as reference</description></item>
    ///   <item><description>Calculate next occurrence using Cronos library</description></item>
    ///   <item><description>Convert result back to UTC for consistent storage</description></item>
    /// </list>
    /// <para>
    /// The method handles daylight saving time transitions by using the configured timezone
    /// and ensures accurate scheduling across time zone changes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Daily at 9 AM Eastern Time
    /// var trigger = CreateTrigger("0 9 * * *", "America/New_York");
    /// var nextExecution = cronTrigger.CalculateNextExecution(trigger, DateTime.UtcNow);
    /// 
    /// // Every 15 minutes
    /// var trigger = CreateTrigger("0 */15 * * * *");
    /// var nextExecution = cronTrigger.CalculateNextExecution(trigger, lastRun);
    /// 
    /// // Using cron descriptors
    /// var trigger = CreateTrigger("@daily");
    /// var nextExecution = cronTrigger.CalculateNextExecution(trigger, null);
    /// </code>
    /// </example>
#pragma warning disable MA0051 // Linear cron expression parsing with timezone conversion and DST handling
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // Cron expression parsing with timezone conversion and multiple fallback paths
    public override DateTime? CalculateNextExecution(IGenericTrigger trigger, DateTime? lastExecution)
    {
        if (trigger?.Configuration == null)
        {
            return null;
        }

        // Extract cron expression
        if (!trigger.Configuration.TryGetValue(CronExpressionKey, out var cronExpressionObj) ||
            cronExpressionObj is not string cronExpression ||
            string.IsNullOrWhiteSpace(cronExpression))
        {
            return null;
        }

        try
        {
            // Parse cron expression
            var cronExpr = CronExpression.Parse(cronExpression);

            // Get timezone, default to UTC
            TimeZoneInfo timeZone = TimeZoneInfo.Utc;
            if (trigger.Configuration.TryGetValue(TimeZoneIdKey, out var timeZoneObj) &&
                timeZoneObj is string timeZoneId &&
                !string.IsNullOrWhiteSpace(timeZoneId))
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }

            // Use the later of lastExecution or current time as reference
            var referenceTime = DateTime.UtcNow;
            if (lastExecution.HasValue && lastExecution.Value > referenceTime)
            {
                referenceTime = lastExecution.Value;
            }

            // Convert reference time to the target timezone for calculation
            var referenceTimeInZone = TimeZoneInfo.ConvertTimeFromUtc(referenceTime, timeZone);

            // Calculate next occurrence in the target timezone
            var nextOccurrence = cronExpr.GetNextOccurrence(referenceTimeInZone, timeZone);

            // Convert back to UTC for storage
            return nextOccurrence?.ToUniversalTime();
        }
        catch (CronFormatException ex)
        {
            // Invalid cron expression format — log and return null so the scheduler skips this trigger
            SchedulingLogger.CalculateNextRunCronFormatFailed(_logger, ex, cronExpression);
            return null;
        }
        catch (TimeZoneNotFoundException ex)
        {
            // Invalid timezone — log warning and fall back to UTC calculation
            SchedulingLogger.CalculateNextRunTimeZoneFailed(_logger, ex, cronExpression);
            try
            {
                var cronExpr = CronExpression.Parse(cronExpression);
                var referenceTime = lastExecution ?? DateTime.UtcNow;
                return cronExpr.GetNextOccurrence(referenceTime, TimeZoneInfo.Utc);
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
            // Other parsing errors — log and return null so the scheduler skips this trigger
            SchedulingLogger.CalculateNextRunArgumentFailed(_logger, ex, cronExpression);
            return null;
        }
    }

    /// <summary>
    /// Determines whether the cron trigger is due to execute at <paramref name="now"/>.
    /// </summary>
    /// <param name="trigger">The trigger carrying the cron expression and optional timezone.</param>
    /// <param name="lastExecution">The timestamp of the last execution, or null if never executed.</param>
    /// <param name="now">The current time.</param>
    /// <returns><c>true</c> if a scheduled occurrence has elapsed since the last run; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// Why: the base <see cref="TriggerTypeBase.IsDue"/> delegates to <see cref="CalculateNextExecution"/>,
    /// which is now-anchored — it returns the NEXT future fire so the scheduler can persist NextRunTime —
    /// so "that future time &lt;= now" can never be true for a recurring cron, and a cron schedule would
    /// never fire. Due-ness instead asks whether a scheduled occurrence has elapsed since the last run:
    /// compute the next occurrence STRICTLY AFTER the last execution and check whether it has arrived. A
    /// never-run schedule is evaluated from one minute ago (the default evaluation cadence) so its first
    /// fire lands at the next boundary without replaying historical occurrences.
    /// </remarks>
    public override bool IsDue(IGenericTrigger trigger, DateTime? lastExecution, DateTimeOffset now)
    {
        if (trigger?.Configuration == null)
        {
            return false;
        }

        if (!trigger.Configuration.TryGetValue(CronExpressionKey, out var cronExpressionObj) ||
            cronExpressionObj is not string cronExpression ||
            string.IsNullOrWhiteSpace(cronExpression))
        {
            return false;
        }

        try
        {
            var cronExpr = CronExpression.Parse(cronExpression);

            TimeZoneInfo timeZone = TimeZoneInfo.Utc;
            if (trigger.Configuration.TryGetValue(TimeZoneIdKey, out var timeZoneObj) &&
                timeZoneObj is string timeZoneId &&
                !string.IsNullOrWhiteSpace(timeZoneId))
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }

            // Next occurrence strictly after the last run (or one minute ago for a never-run schedule);
            // due once that occurrence is at or before now.
            var fromUtc = lastExecution ?? now.UtcDateTime.AddMinutes(-1);
            var fromInZone = TimeZoneInfo.ConvertTimeFromUtc(fromUtc, timeZone);
            var nextOccurrence = cronExpr.GetNextOccurrence(fromInZone, timeZone)?.ToUniversalTime();

            return nextOccurrence.HasValue && nextOccurrence.Value <= now.UtcDateTime;
        }
        catch (CronFormatException ex)
        {
            SchedulingLogger.CalculateNextRunCronFormatFailed(_logger, ex, cronExpression);
            return false;
        }
        catch (TimeZoneNotFoundException ex)
        {
            SchedulingLogger.CalculateNextRunTimeZoneFailed(_logger, ex, cronExpression);
            return false;
        }
        catch (ArgumentException ex)
        {
            SchedulingLogger.CalculateNextRunArgumentFailed(_logger, ex, cronExpression);
            return false;
        }
    }
#pragma warning restore MA0051

    /// <summary>
    /// Validates that the trigger configuration contains a valid cron expression and timezone.
    /// </summary>
    /// <param name="trigger">The trigger configuration to validate.</param>
    /// <returns>
    /// A success result if the trigger is valid, or an error result with validation messages if invalid.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs comprehensive validation of the cron trigger configuration:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><strong>Cron expression validation</strong>: Ensures the expression is valid Cronos format</description></item>
    ///   <item><description><strong>Timezone validation</strong>: Verifies timezone ID exists if provided</description></item>
    ///   <item><description><strong>Configuration completeness</strong>: Ensures required parameters are present</description></item>
    /// </list>
    /// <para>
    /// The validation uses the Cronos library to parse the cron expression, ensuring it will work
    /// correctly during execution. Invalid expressions or timezones result in detailed error messages.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Valid cron trigger
    /// var validTrigger = CreateTrigger("0 9 * * MON-FRI", "America/New_York");
    /// var result = cronTrigger.ValidateTrigger(validTrigger);
    /// // result.Success == true
    /// 
    /// // Invalid cron expression
    /// var invalidTrigger = CreateTrigger("invalid cron", "UTC");
    /// var result = cronTrigger.ValidateTrigger(invalidTrigger);
    /// // result.Error == true, result.Messages contains validation errors
    /// 
    /// // Invalid timezone
    /// var badTimezoneTrigger = CreateTrigger("0 9 * * *", "Invalid/Timezone");
    /// var result = cronTrigger.ValidateTrigger(badTimezoneTrigger);
    /// // result.Error == true with timezone validation error
    /// </code>
    /// </example>
#pragma warning disable MA0051 // Linear cron validation with expression and timezone verification
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // Validation logic — cron expression parsing with timezone verification and multiple checks
    public override IGenericResult ValidateTrigger(IGenericTrigger trigger)
    {
        if (trigger?.Configuration == null)
        {
            return GenericResult.Failure(SchedulingLogger.TriggerConfigurationNull(_logger));
        }

        // Validate cron expression is present
        if (!trigger.Configuration.TryGetValue(CronExpressionKey, out var cronExpressionObj) ||
            cronExpressionObj is not string cronExpression ||
            string.IsNullOrWhiteSpace(cronExpression))
        {
            return GenericResult.Failure(SchedulingLogger.CronExpressionRequired(_logger, CronExpressionKey));
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

        // Validate that the cron expression will actually fire (not just syntactically valid)
        try
        {
            var cronExpr = CronExpression.Parse(cronExpression);
            var timeZone = TimeZoneInfo.Utc;

            if (trigger.Configuration.TryGetValue(TimeZoneIdKey, out var tzObj) &&
                tzObj is string tzId &&
                !string.IsNullOrWhiteSpace(tzId))
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            }

            var nextOccurrence = cronExpr.GetNextOccurrence(DateTime.UtcNow, timeZone);
            if (!nextOccurrence.HasValue)
            {
                return GenericResult.Failure(SchedulingLogger.CronExpressionWillNeverExecute(_logger, cronExpression));
            }
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(SchedulingLogger.CronExpressionValidationFailed(_logger, ex.Message, cronExpression));
        }

        return GenericResult.Success();
    }
#pragma warning restore MA0051

    /// <summary>
    /// Calculates the next run time for this cron trigger, returning a structured result.
    /// </summary>
    /// <param name="trigger">The trigger configuration containing the cron expression and optional timezone.</param>
    /// <param name="lastExecution">The timestamp of the last execution, or null if never executed.</param>
    /// <returns>
    /// A success result containing the next execution time in UTC, or a failure result if the
    /// cron expression is invalid or will never execute.
    /// </returns>
#pragma warning disable MA0051 // Linear cron expression parsing with timezone conversion and DST handling
    [ConventionOverride(MaxCyclomaticComplexity = 20)]
    public override IGenericResult<DateTimeOffset> GetNextRunTime(IGenericTrigger trigger, DateTime? lastExecution)
    {
        if (trigger?.Configuration == null)
        {
            return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.TriggerConfigurationNull(_logger));
        }

        if (!trigger.Configuration.TryGetValue(CronExpressionKey, out var cronExpressionObj) ||
            cronExpressionObj is not string cronExpression ||
            string.IsNullOrWhiteSpace(cronExpression))
        {
            return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.CronExpressionRequired(_logger, CronExpressionKey));
        }

        try
        {
            var cronExpr = CronExpression.Parse(cronExpression);

            TimeZoneInfo timeZone = TimeZoneInfo.Utc;
            if (trigger.Configuration.TryGetValue(TimeZoneIdKey, out var timeZoneObj) &&
                timeZoneObj is string timeZoneId &&
                !string.IsNullOrWhiteSpace(timeZoneId))
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }

            var referenceTime = DateTime.UtcNow;
            if (lastExecution.HasValue && lastExecution.Value > referenceTime)
            {
                referenceTime = lastExecution.Value;
            }

            var referenceTimeInZone = TimeZoneInfo.ConvertTimeFromUtc(referenceTime, timeZone);
            var nextOccurrence = cronExpr.GetNextOccurrence(referenceTimeInZone, timeZone);

            if (!nextOccurrence.HasValue)
            {
                return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.CronExpressionWillNeverExecute(_logger, cronExpression));
            }

            return GenericResult<DateTimeOffset>.Success(new DateTimeOffset(nextOccurrence.Value.ToUniversalTime(), TimeSpan.Zero));
        }
        catch (CronFormatException ex)
        {
            return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.InvalidCronExpressionFormat(_logger, ex.Message, cronExpression));
        }
        catch (TimeZoneNotFoundException ex)
        {
            return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.InvalidTimezoneIdentifier(_logger, ex.Message));
        }
        catch (ArgumentException ex)
        {
            return GenericResult<DateTimeOffset>.Failure(SchedulingLogger.InvalidCronExpression(_logger, ex.Message, cronExpression));
        }
    }
#pragma warning restore MA0051
}