using Fdw.Services.Scheduling.Abstractions.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Abstractions.Tests.Results;

public class SchedulingResultCodesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void AllReturnsAllResultCodes()
    {
        var all = SchedulingResultCodes.All();

        all.ShouldNotBeEmpty();
        all.Count.ShouldBeGreaterThanOrEqualTo(7); // At least the 7 defined codes
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ByIdReturnsCorrectResultCode()
    {
        var cronRequired = SchedulingResultCodes.CronExpressionRequired;
        var result = SchedulingResultCodes.ById(cronRequired.Id);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(cronRequired.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        var result = SchedulingResultCodes.ById(99999);

        result.ShouldNotBeNull();
        result.ShouldBe(SchedulingResultCodes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ByNameReturnsCronExpressionRequired()
    {
        var result = SchedulingResultCodes.ByName("CronExpressionRequired");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("CronExpressionRequired");
        // Catalog invariant: Code == "{prefix}-{number}", Id == EventId == number, Domain == prefix.
        result.Code.ShouldBe($"SCHEDULING-{result.Id}");
        result.EventId.ShouldBe(result.Id);
        result.Domain.ShouldBe("SCHEDULING");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ByNameReturnsCronExpressionInvalidFieldCount()
    {
        var result = SchedulingResultCodes.ByName("CronExpressionInvalidFieldCount");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("CronExpressionInvalidFieldCount");
        result.Code.ShouldBe($"SCHEDULING-{result.Id}");
        result.EventId.ShouldBe(result.Id);
        result.Domain.ShouldBe("SCHEDULING");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ByNameReturnsIntervalMinutesRequired()
    {
        var result = SchedulingResultCodes.ByName("IntervalMinutesRequired");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("IntervalMinutesRequired");
        result.Code.ShouldBe($"SCHEDULING-{result.Id}");
        result.EventId.ShouldBe(result.Id);
        result.Domain.ShouldBe("SCHEDULING");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ByNameReturnsIntervalStartTimeInvalid()
    {
        var result = SchedulingResultCodes.ByName("IntervalStartTimeInvalid");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("IntervalStartTimeInvalid");
        result.Code.ShouldBe($"SCHEDULING-{result.Id}");
        result.EventId.ShouldBe(result.Id);
        result.Domain.ShouldBe("SCHEDULING");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ByNameReturnsOnceExecuteAtRequired()
    {
        var result = SchedulingResultCodes.ByName("OnceExecuteAtRequired");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("OnceExecuteAtRequired");
        result.Code.ShouldBe($"SCHEDULING-{result.Id}");
        result.EventId.ShouldBe(result.Id);
        result.Domain.ShouldBe("SCHEDULING");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ByNameReturnsOnceExecuteAtMustBeUtc()
    {
        var result = SchedulingResultCodes.ByName("OnceExecuteAtMustBeUtc");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("OnceExecuteAtMustBeUtc");
        result.Code.ShouldBe($"SCHEDULING-{result.Id}");
        result.EventId.ShouldBe(result.Id);
        result.Domain.ShouldBe("SCHEDULING");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ByNameReturnsManualAllowConcurrentInvalid()
    {
        var result = SchedulingResultCodes.ByName("ManualAllowConcurrentInvalid");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("ManualAllowConcurrentInvalid");
        result.Code.ShouldBe($"SCHEDULING-{result.Id}");
        result.EventId.ShouldBe(result.Id);
        result.Domain.ShouldBe("SCHEDULING");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ByNameReturnsUnknownTriggerType()
    {
        var result = SchedulingResultCodes.ByName("UnknownTriggerType");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("UnknownTriggerType");
        result.Code.ShouldBe($"SCHEDULING-{result.Id}");
        result.EventId.ShouldBe(result.Id);
        result.Domain.ShouldBe("SCHEDULING");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ByNameIsCaseSensitive()
    {
        SchedulingResultCodes.ByName("CronExpressionRequired").ShouldNotBeNull();
        SchedulingResultCodes.ByName("cronexpressionrequired").ShouldBe(SchedulingResultCodes.NotFound);
        SchedulingResultCodes.ByName("CRONEXPRESSIONREQUIRED").ShouldBe(SchedulingResultCodes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        var result = SchedulingResultCodes.ByName("UnknownCode");

        result.ShouldNotBeNull();
        result.ShouldBe(SchedulingResultCodes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void NotFoundReturnsEmptyInstance()
    {
        var result = SchedulingResultCodes.NotFound;

        result.ShouldNotBeNull();
        result.Name.ShouldBe("NotFound");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public void AllResultCodesHaveUniqueIds()
    {
        var all = SchedulingResultCodes.All();
        var ids = all.Select(r => r.Id).ToList();

        ids.Distinct().Count().ShouldBe(ids.Count);
    }

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
    public void AllResultCodesHaveSchedulingDomain()
    {
        var all = SchedulingResultCodes.All();

        foreach (var code in all)
        {
            // Catalog invariant: Domain == prefix passed to the categorized base ctor ("SCHEDULING").
            // Skip the NotFound sentinel (Code=="UNKNOWN", no prefix domain).
            if (string.Equals(code.Name, "NotFound", System.StringComparison.Ordinal))
            {
                continue;
            }

            code.Domain.ShouldBe("SCHEDULING");
        }
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
    }
}
