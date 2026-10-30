using System;
using System.Linq;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for when a run that a pipeline holds past its limit joins the queue (FDW-3129): a delay in seconds, or the next occurrence of a time
/// of day in a time zone, and every value that makes the pair unusable named by its field.
/// </summary>
public sealed class PipelineDelayedStartTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 10, 0, 0, TimeSpan.Zero);

    private static PipelineDelayedStart Read(int? seconds, string? timeOfDay, string? zone)
    {
        var problems = PipelineDelayedStart.TryRead(seconds, timeOfDay, zone, out var delayedStart);
        problems.ShouldBeEmpty();
        return delayedStart.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ADelayIsDueThatManySecondsAfterTheTrigger()
    {
        Read(600, null, null).DueAt(Now).ShouldBe(Now.AddMinutes(10));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ATimeOfDayLaterTodayIsDueTodayInItsTimeZone()
    {
        Read(null, "14:30", "UTC").DueAt(Now).ShouldBe(new DateTimeOffset(2026, 10, 29, 14, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ATimeOfDayThatHasPassedIsDueTomorrow()
    {
        Read(null, "09:00", "UTC").DueAt(Now).ShouldBe(new DateTimeOffset(2026, 10, 30, 9, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ATimeOfDayIsReadInTheZoneThePipelineNames()
    {
        // 10:00 UTC on 2026-10-29 is 06:00 in New York (UTC-4), so 02:00 there is the next morning, at 06:00 UTC.
        Read(null, "02:00", "America/New_York").DueAt(Now).ShouldBe(new DateTimeOffset(2026, 10, 30, 6, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ATimeOfDayThatTheClocksJumpOverIsTheFirstMomentAfterTheJump()
    {
        // New York, 2026-03-08: at 02:00 the clocks go to 03:00, so 02:30 does not exist. 03:00 EDT is 07:00 UTC.
        var before = new DateTimeOffset(2026, 3, 8, 5, 0, 0, TimeSpan.Zero);

        Read(null, "02:30", "America/New_York").DueAt(before).ShouldBe(new DateTimeOffset(2026, 3, 8, 7, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ATimeOfDayThatOccursTwiceIsItsFirstOccurrence()
    {
        // New York, 2026-11-01: at 02:00 EDT the clocks go back to 01:00 EST, so 01:30 happens twice. The first is 01:30 EDT, 05:30 UTC.
        var before = new DateTimeOffset(2026, 11, 1, 4, 0, 0, TimeSpan.Zero);

        Read(null, "01:30", "America/New_York").DueAt(before).ShouldBe(new DateTimeOffset(2026, 11, 1, 5, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AValueThatIsNotStatedIsNamedWhenNeitherOrBothAreGiven()
    {
        PipelineDelayedStart.TryRead(null, null, null, out var neither).Select(problem => problem.Field).ShouldBe(["delayedStartSeconds"]);
        PipelineDelayedStart.TryRead(60, "02:00", "UTC", out var both).Select(problem => problem.Field).ShouldBe(["delayedStartSeconds"]);

        neither.ShouldBeNull();
        both.ShouldBeNull();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData(0, null, null, "delayedStartSeconds")]
    [InlineData(-5, null, null, "delayedStartSeconds")]
    [InlineData(2_592_001, null, null, "delayedStartSeconds")]
    [InlineData(60, null, "UTC", "delayedStartTimeZoneId")]
    [InlineData(null, "25:00", "UTC", "delayedStartTimeOfDay")]
    [InlineData(null, "7:5", "UTC", "delayedStartTimeOfDay")]
    [InlineData(null, "02:00", null, "delayedStartTimeZoneId")]
    [InlineData(null, "02:00", " ", "delayedStartTimeZoneId")]
    [InlineData(null, "02:00", "Mars/Olympus", "delayedStartTimeZoneId")]
    public void AValueThatCannotBeUsedIsNamedByItsField(int? seconds, string? timeOfDay, string? zone, string field)
    {
        var problems = PipelineDelayedStart.TryRead(seconds, timeOfDay, zone, out var delayedStart);

        problems.Select(problem => problem.Field).ShouldContain(field);
        delayedStart.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ABehaviorThatDoesNotHoldARunNamesEachValueStated()
    {
        PipelineDelayedStart.Unexpected(null, null, null, "Queue").ShouldBeEmpty();

        var problems = PipelineDelayedStart.Unexpected(60, "02:00", "UTC", "Refuse");

        problems.Select(problem => problem.Field).ShouldBe(["delayedStartSeconds", "delayedStartTimeOfDay", "delayedStartTimeZoneId"]);
        problems.ShouldAllBe(problem => problem.Message.Contains("Refuse", StringComparison.Ordinal));
    }
}
