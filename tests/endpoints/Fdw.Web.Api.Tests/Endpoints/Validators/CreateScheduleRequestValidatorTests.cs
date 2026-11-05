using Fdw.Services.Scheduling.Endpoints;
using Fdw.Services.Scheduling.Endpoints.Validators;

namespace Fdw.Web.Api.Tests.Endpoints.Validators;

/// <summary>Tests for the create-schedule rules every host inherits (FDW-3572).</summary>
public sealed class CreateScheduleRequestValidatorTests
{
    private sealed class Sut : CreateScheduleRequestValidator;

    private static CreateScheduleRequest Cron() => new()
    {
        Name = "nightly-load",
        PipelineId = Guid.NewGuid(),
        PipelineName = "Load",
        SchedulerType = "Cron",
        CronExpression = "0 2 * * *",
        MissedFireRule = "FireOnce",
        RunRoles = ["Admin"],
    };

    [Fact]
    public void AValidCronScheduleIsAccepted() => new Sut().Validate(Cron()).IsValid.ShouldBeTrue();

    [Fact]
    public void ANameOver200CharactersIsRefused()
    {
        var request = Cron();
        request.Name = "a" + new string('b', 200);

        new Sut().Validate(request).Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void ARequestWithNoRunRolesIsRefused()
    {
        var request = Cron();
        request.RunRoles = [];

        new Sut().Validate(request).Errors.ShouldContain(e => e.PropertyName == "RunRoles");
    }

    [Fact]
    public void ASchedulerTypeThatIsNotATriggerTypeIsRefused()
    {
        var request = Cron();
        request.SchedulerType = "Sometimes";

        new Sut().Validate(request).Errors.ShouldContain(e => e.PropertyName == "SchedulerType");
    }

    [Fact]
    public void ACronScheduleWithAMalformedExpressionIsRefused()
    {
        var request = Cron();
        request.CronExpression = "not a cron";

        new Sut().Validate(request).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void AnEmptyCronExpressionAnswersOnce()
    {
        var request = Cron();
        request.CronExpression = string.Empty;

        new Sut().Validate(request).Errors.Count(e => e.PropertyName == "CronExpression").ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(86401)]
    public void AnIntervalOutsideOneSecondToADayIsRefused(int seconds)
    {
        var request = Cron();
        request.SchedulerType = "Interval";
        request.IntervalSeconds = seconds;

        new Sut().Validate(request).Errors.ShouldContain(e => e.PropertyName == "IntervalSeconds");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ACronExpressionOverTheColumnsLimitIsRefused()
    {
        var request = Cron();
        request.CronExpression = new string('*', 256);

        new Sut().Validate(request).Errors.ShouldContain(e => e.PropertyName == "CronExpression" && e.ErrorMessage == "Cron expression cannot exceed 255 characters");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AnEventNameOverTheColumnsLimitIsRefused()
    {
        var request = Cron();
        request.SchedulerType = "Event";
        request.EventName = new string('e', 201);

        new Sut().Validate(request).Errors.ShouldContain(e => e.PropertyName == "EventName");
    }
}
