using Fdw.Services.Scheduling.Abstractions.Models;
using Fdw.Services.Scheduling.Abstractions.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Abstractions.Tests.Models;

/// <summary>
/// Tests for <see cref="ScheduleTimeZone"/> (FDW-1170): a stated time zone is accepted exactly when the scheduler can resolve it,
/// and a refusal is the coded 400 that names the value.
/// </summary>
public class ScheduleTimeZoneTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void AZoneThatIsNotStatedIsNotJudged()
    {
        ScheduleTimeZone.Check(null, NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("UTC")]
    [InlineData("America/Chicago")]
    [InlineData("Europe/Paris")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void AZoneTheSchedulerCanResolveIsAccepted(string zone)
    {
        ScheduleTimeZone.Check(zone, NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Not/AZone")]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void AZoneTheSchedulerCannotResolveIsRefusedWithTheTimeZoneCode(string zone)
    {
        var result = ScheduleTimeZone.Check(zone, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeSameAs(SchedulingResultCodes.ScheduleTimeZoneInvalid);
        result.Code!.Id.ShouldBe(21006);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void ACreateThatDoesNotStateAZoneIsRefusedWithTheTimeZoneCode()
    {
        var result = ScheduleTimeZone.Require(null, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeSameAs(SchedulingResultCodes.ScheduleTimeZoneInvalid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Not/AZone")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void ACreateThatStatesABlankOrUnknownZoneIsRefused(string zone)
    {
        ScheduleTimeZone.Require(zone, NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData("UTC")]
    [InlineData("America/Chicago")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void ACreateThatStatesAZoneTheSchedulerKnowsIsAccepted(string zone)
    {
        ScheduleTimeZone.Require(zone, NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void TheRefusalNamesTheValueTheCallerSent()
    {
        var result = ScheduleTimeZone.Check("Not/AZone", NullLogger.Instance);

        SchedulingResultCodes.ScheduleTimeZoneInvalid.FormatClientMessage(result.Details).ShouldNotBeNull().ShouldContain("Not/AZone");
    }
}
