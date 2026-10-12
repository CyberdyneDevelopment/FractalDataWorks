using System;
using Fdw.Services.Scheduling.Abstractions.Models;
using Fdw.Services.Scheduling.Abstractions.OptionTypes;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Abstractions.Tests.OptionTypes;

public class TriggerTypesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CronPropertyReturnsInstance()
    {
        var result = TriggerTypes.Cron;

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Cron");
        result.RequiresSchedule.ShouldBeTrue();
        result.IsImmediate.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void IntervalPropertyReturnsInstance()
    {
        var result = TriggerTypes.Interval;

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Interval");
        result.RequiresSchedule.ShouldBeTrue();
        result.IsImmediate.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ManualPropertyReturnsInstance()
    {
        var result = TriggerTypes.Manual;

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Manual");
        result.RequiresSchedule.ShouldBeFalse();
        result.IsImmediate.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void OncePropertyReturnsInstance()
    {
        var result = TriggerTypes.Once;

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Once");
        result.RequiresSchedule.ShouldBeFalse(); // Once triggers do not require schedule persistence
        result.IsImmediate.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void IntervalIsDueWithNullLastExecutionIsDueImmediately()
    {
        var trigger = Trigger.CreateInterval("FirstRun", intervalMinutes: 5);

        TriggerTypes.Interval.IsDue(trigger, lastExecution: null, DateTimeOffset.UtcNow).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void IntervalIsDueWithRecentLastExecutionIsNotDue()
    {
        var trigger = Trigger.CreateInterval("Cadence", intervalMinutes: 5);
        var now = DateTimeOffset.UtcNow;

        TriggerTypes.Interval.IsDue(trigger, now.UtcDateTime.AddMinutes(-1), now).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void IntervalIsDueWithElapsedIntervalIsDue()
    {
        var trigger = Trigger.CreateInterval("Cadence", intervalMinutes: 5);
        var now = DateTimeOffset.UtcNow;

        TriggerTypes.Interval.IsDue(trigger, now.UtcDateTime.AddMinutes(-6), now).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void IntervalIsDueWithNullLastExecutionAndInvalidConfigurationIsNotDue()
    {
        TriggerTypes.Interval.IsDue(Trigger.CreateManual("Broken"), lastExecution: null, DateTimeOffset.UtcNow)
            .ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void EventPropertyReturnsInstance()
    {
        var result = TriggerTypes.Event;

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Event");
        result.RequiresSchedule.ShouldBeFalse();
        result.IsImmediate.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void EventCalculateNextExecutionIsAlwaysNull()
    {
        var trigger = Trigger.CreateEvent("Post-extract", eventName: "ExtractCompleted");

        TriggerTypes.Event.CalculateNextExecution(trigger, lastExecution: null).ShouldBeNull();
        TriggerTypes.Event.CalculateNextExecution(trigger, DateTime.UtcNow.AddDays(-1)).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void EventIsNeverDueOnTheClock()
    {
        var trigger = Trigger.CreateEvent("Post-extract", eventName: "ExtractCompleted");
        var now = DateTimeOffset.UtcNow;

        TriggerTypes.Event.IsDue(trigger, lastExecution: null, now).ShouldBeFalse();
        TriggerTypes.Event.IsDue(trigger, now.UtcDateTime.AddYears(-1), now).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void EventValidateTriggerSucceedsWithEventName()
    {
        var trigger = Trigger.CreateEvent("Post-extract", eventName: "ExtractCompleted");

        TriggerTypes.Event.ValidateTrigger(trigger).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void EventValidateTriggerFailsWhenEventNameMissing()
    {
        TriggerTypes.Event.ValidateTrigger(Trigger.CreateManual("NoEventName")).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void EventGetNextRunTimeFails()
    {
        var trigger = Trigger.CreateEvent("Post-extract", eventName: "ExtractCompleted");

        TriggerTypes.Event.GetNextRunTime(trigger, lastExecution: null).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void CreateEventPopulatesTypeAndConfiguration()
    {
        var trigger = Trigger.CreateEvent("Post-extract", eventName: "ExtractCompleted", description: "After extract");

        trigger.TriggerType.ShouldBe("Event");
        trigger.TriggerName.ShouldBe("Post-extract");
        trigger.Configuration["EventName"].ShouldBe("ExtractCompleted");
        trigger.Configuration["Description"].ShouldBe("After extract");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void CreateEventRejectsBlankEventName()
    {
        Should.Throw<ArgumentException>(() => Trigger.CreateEvent("Post-extract", eventName: "   "));
    }
}
