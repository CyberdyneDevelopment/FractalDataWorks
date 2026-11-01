using Fdw.Services.Scheduling.Abstractions.Models;
using Fdw.Services.Scheduling.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Scheduling;

/// <summary>
/// Tests that no schedule request assumes a time zone (FDW-2059): a create that does not state one is refused, never run in UTC.
/// </summary>
public sealed class ScheduleTimeZoneDefaultTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ACreateRequestThatStatesNoZoneIsRefused()
    {
        var request = new CreateScheduleRequest();

        request.TimeZoneId.ShouldBeEmpty();
        ScheduleTimeZone.Require(request.TimeZoneId, NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AProxyCreateRequestThatStatesNoZoneIsRefused()
    {
        var request = new ProxyCreateScheduleRequest();

        request.TimeZoneId.ShouldBeEmpty();
        ScheduleTimeZone.Require(request.TimeZoneId, NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AZoneTheCallerStatesIsKeptAsStated()
    {
        var request = new CreateScheduleRequest { TimeZoneId = "America/Chicago" };

        ScheduleTimeZone.Require(request.TimeZoneId, NullLogger.Instance).IsSuccess.ShouldBeTrue();
        request.TimeZoneId.ShouldBe("America/Chicago");
    }
}
