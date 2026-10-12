using Fdw.Services.Scheduling.Abstractions.Models;
using Fdw.Services.Scheduling.Abstractions.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Abstractions.Tests.Models;

/// <summary>
/// Tests for <see cref="ScheduleTypeChange"/> (FDW-1171): a scheduler type an update states is accepted only when it is the
/// stored one, and any other is the coded 400 that names both types; it is never accepted and ignored.
/// </summary>
public class ScheduleTypeChangeTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void ATypeThatIsNotStatedIsNotJudged()
    {
        ScheduleTypeChange.Check(null, "Cron", NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Cron", "Cron")]
    [InlineData("cron", "Cron")]
    [InlineData("INTERVAL", "Interval")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void ATypeThatNamesTheStoredOneChangesNothingAndIsAccepted(string stated, string stored)
    {
        ScheduleTypeChange.Check(stated, stored, NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Interval", "Cron")]
    [InlineData("Manual", "Interval")]
    [InlineData("Cron", "Manual")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void ADifferentTypeIsRefusedWithTheTypeChangeCode(string stated, string stored)
    {
        var result = ScheduleTypeChange.Check(stated, stored, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeSameAs(SchedulingResultCodes.ScheduleTypeChangeRefused);
        result.Code!.Id.ShouldBe(21007);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void TheRefusalNamesBothTypes()
    {
        var result = ScheduleTypeChange.Check("Interval", "Cron", NullLogger.Instance);

        var message = SchedulingResultCodes.ScheduleTypeChangeRefused.FormatClientMessage(result.Details).ShouldNotBeNull();
        message.ShouldContain("Interval");
        message.ShouldContain("Cron");
    }
}
