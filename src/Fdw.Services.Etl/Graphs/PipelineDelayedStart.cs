using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// When a run that a pipeline holds past its limit joins the queue (FDW-3129): after a set number of seconds, or at the next occurrence of a
/// time of day in a time zone the pipeline names.
/// </summary>
/// <remarks>
/// <para>
/// A pipeline whose overflow behavior holds a run states exactly one of the two, and a time of day goes with its time zone. No value is
/// assumed: a pipeline that states neither, both, or a value that is not usable has a problem for each, and no run is held on a guess.
/// A pipeline whose overflow behavior does not hold a run states none of the three.
/// </para>
/// <para>
/// A time of day that does not occur on a day because the clocks jump over it is the first moment after the jump. A time of day that occurs
/// twice because the clocks go back is its first occurrence. A time of day that has just passed is tomorrow's.
/// </para>
/// </remarks>
public sealed class PipelineDelayedStart
{
    /// <summary>The longest delay, in seconds: 30 days. A longer hold is not a delay, and a timer cannot wait longer than about 49 days.</summary>
    public const int MaximumSeconds = 2_592_000;

    private const string TimeOfDayFormat = "HH:mm";

    private readonly Func<DateTimeOffset, DateTimeOffset> _due;

    private PipelineDelayedStart(string description, Func<DateTimeOffset, DateTimeOffset> due)
    {
        Description = description;
        _due = due;
    }

    /// <summary>Gets what the pipeline states, as a log or a message names it.</summary>
    public string Description { get; }

    /// <summary>Reads what a pipeline that holds a run states.</summary>
    /// <param name="seconds">The delay in seconds, or <see langword="null"/>.</param>
    /// <param name="timeOfDay">The time of day as <c>HH:mm</c>, or <see langword="null"/>.</param>
    /// <param name="timeZoneId">The time zone id, or <see langword="null"/>.</param>
    /// <param name="delayedStart">The delay when nothing is wrong; otherwise <see langword="null"/>.</param>
    /// <returns>What is wrong, by the name of the field, or an empty list when the delay can be read.</returns>
    public static IReadOnlyList<(string Field, string Message)> TryRead(int? seconds, string? timeOfDay, string? timeZoneId, out PipelineDelayedStart? delayedStart)
    {
        delayedStart = null;
        var problems = new List<(string, string)>();
        if (seconds is null && timeOfDay is null)
        {
            problems.Add(("delayedStartSeconds", "it holds a run past its limit but states neither a delay (delayedStartSeconds) nor a time of day (delayedStartTimeOfDay)"));
        }
        else if (seconds is not null && timeOfDay is not null)
        {
            problems.Add(("delayedStartSeconds", "it states both a delay (delayedStartSeconds) and a time of day (delayedStartTimeOfDay); it states one"));
        }
        else if (seconds is { } delay)
        {
            delayedStart = AfterSeconds(delay, timeZoneId, problems);
        }
        else
        {
            delayedStart = AtTimeOfDay(timeOfDay, timeZoneId, problems);
        }

        return problems;
    }

    /// <summary>Names each of the three values that a pipeline whose overflow behavior does not hold a run states.</summary>
    /// <param name="seconds">The delay in seconds, or <see langword="null"/>.</param>
    /// <param name="timeOfDay">The time of day, or <see langword="null"/>.</param>
    /// <param name="timeZoneId">The time zone id, or <see langword="null"/>.</param>
    /// <param name="behavior">The name of the overflow behavior.</param>
    /// <returns>A problem for each value stated.</returns>
    public static IReadOnlyList<(string Field, string Message)> Unexpected(int? seconds, string? timeOfDay, string? timeZoneId, string behavior)
    {
        var problems = new List<(string, string)>();
        AddIfStated(problems, "delayedStartSeconds", seconds is not null, behavior);
        AddIfStated(problems, "delayedStartTimeOfDay", timeOfDay is not null, behavior);
        AddIfStated(problems, "delayedStartTimeZoneId", timeZoneId is not null, behavior);
        return problems;
    }

    /// <summary>Works out when a run held now joins the queue.</summary>
    /// <param name="now">When the run was triggered.</param>
    /// <returns>The instant it joins the queue.</returns>
    public DateTimeOffset DueAt(DateTimeOffset now) => _due(now);

    private static void AddIfStated(List<(string, string)> problems, string field, bool stated, string behavior)
    {
        if (stated)
        {
            problems.Add((field, $"{field} is stated, and the overflow behavior is '{behavior}': only DelayedStart uses it"));
        }
    }

    private static PipelineDelayedStart? AfterSeconds(int seconds, string? timeZoneId, List<(string, string)> problems)
    {
        if (seconds < 1 || seconds > MaximumSeconds)
        {
            problems.Add(("delayedStartSeconds", $"delayedStartSeconds is {seconds}, which is not between 1 and {MaximumSeconds} (30 days)"));
        }

        if (timeZoneId is not null)
        {
            problems.Add(("delayedStartTimeZoneId", "delayedStartTimeZoneId is stated without a time of day: a delay in seconds has no time zone"));
        }

        return problems.Count > 0
            ? null
            : new PipelineDelayedStart(string.Create(CultureInfo.InvariantCulture, $"{seconds} seconds"), now => now.AddSeconds(seconds));
    }

    private static PipelineDelayedStart? AtTimeOfDay(string? timeOfDay, string? timeZoneId, List<(string, string)> problems)
    {
        var parsed = TimeOnly.TryParseExact(timeOfDay, TimeOfDayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time);
        if (!parsed)
        {
            problems.Add(("delayedStartTimeOfDay", $"delayedStartTimeOfDay is '{timeOfDay}', which is not a time of day in the form {TimeOfDayFormat} on 24 hours"));
        }

        TimeZoneInfo? zone = null;
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            problems.Add(("delayedStartTimeZoneId", "delayedStartTimeZoneId is missing: a time of day is read in a time zone the pipeline states"));
        }
        else if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out zone))
        {
            problems.Add(("delayedStartTimeZoneId", $"delayedStartTimeZoneId is '{timeZoneId}', which is not a time zone this host knows"));
        }

        return problems.Count > 0 || zone is null
            ? null
            : new PipelineDelayedStart(string.Concat(timeOfDay, " ", zone.Id), now => NextOccurrence(now, time, zone));
    }

    private static DateTimeOffset NextOccurrence(DateTimeOffset now, TimeOnly time, TimeZoneInfo zone)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
        var first = At(today, time, zone);
        return first > now ? first : At(today.AddDays(1), time, zone);
    }

    private static DateTimeOffset At(DateOnly day, TimeOnly time, TimeZoneInfo zone)
    {
        var wall = day.ToDateTime(time);
        while (zone.IsInvalidTime(wall))
        {
            wall = wall.AddMinutes(1);
        }

        var offset = zone.IsAmbiguousTime(wall) ? zone.GetAmbiguousTimeOffsets(wall).Max() : zone.GetUtcOffset(wall);
        return new DateTimeOffset(wall, offset);
    }
}
