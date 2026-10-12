using Fdw.Services.Scheduling.Abstractions.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Abstractions.Tests.Results;

public class SchedulingResultCodesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void AllResultCodesHaveUniqueNames()
    {
        var all = SchedulingResultCodes.All();
        var names = all.Select(r => r.Name).ToList();

        names.Distinct().Count().ShouldBe(names.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void AllResultCodesHaveUniqueCodes()
    {
        var all = SchedulingResultCodes.All();
        var codes = all.Select(r => r.Code).ToList();

        codes.Distinct().Count().ShouldBe(codes.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void AllResultCodesHaveUniqueEventIds()
    {
        var all = SchedulingResultCodes.All();
        var eventIds = all.Select(r => r.EventId).ToList();

        eventIds.Distinct().Count().ShouldBe(eventIds.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void StaticPropertiesReturnCorrectInstances()
    {
        SchedulingResultCodes.CronExpressionRequired.Name.ShouldBe("CronExpressionRequired");
        SchedulingResultCodes.CronExpressionInvalidFieldCount.Name.ShouldBe("CronExpressionInvalidFieldCount");
        SchedulingResultCodes.IntervalMinutesRequired.Name.ShouldBe("IntervalMinutesRequired");
        SchedulingResultCodes.IntervalStartTimeInvalid.Name.ShouldBe("IntervalStartTimeInvalid");
        SchedulingResultCodes.OnceExecuteAtRequired.Name.ShouldBe("OnceExecuteAtRequired");
        SchedulingResultCodes.OnceExecuteAtMustBeUtc.Name.ShouldBe("OnceExecuteAtMustBeUtc");
        SchedulingResultCodes.ManualAllowConcurrentInvalid.Name.ShouldBe("ManualAllowConcurrentInvalid");
        SchedulingResultCodes.UnknownTriggerType.Name.ShouldBe("UnknownTriggerType");
        SchedulingResultCodes.ScheduleTimeZoneInvalid.Name.ShouldBe("ScheduleTimeZoneInvalid");
        SchedulingResultCodes.ScheduleTypeChangeRefused.Name.ShouldBe("ScheduleTypeChangeRefused");
    }
}
