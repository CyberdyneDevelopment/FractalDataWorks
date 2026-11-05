using Fdw.Services.Scheduling.Endpoints;
using Fdw.Services.Scheduling.Endpoints.Validators;

namespace Fdw.Web.Api.Tests.Endpoints.Validators;

/// <summary>Tests for the update-schedule rules every host inherits (FDW-3572).</summary>
public sealed class UpdateScheduleRequestValidatorTests
{
    private sealed class Sut : UpdateScheduleRequestValidator;

    [Fact]
    public void ARequestThatStatesACronExpressionPasses() =>
        new Sut().Validate(new UpdateScheduleRequest { Name = "nightly-load", CronExpression = "0 3 * * *" }).IsValid.ShouldBeTrue();

    [Fact]
    public void ARequestThatStatesNothingIsRefused() =>
        new Sut().Validate(new UpdateScheduleRequest { Name = "nightly-load" }).Errors
            .ShouldContain(e => e.ErrorCode == UpdateScheduleChanges.NothingStatedCode);

    [Fact]
    public void AStoredNameInAnotherFormIsNotRefusedByANameRule() =>
        new Sut().Validate(new UpdateScheduleRequest { Name = "old name 1", CronExpression = "0 3 * * *" }).IsValid.ShouldBeTrue();

    [Fact]
    public void APipelineNameWithoutAPipelineIdIsRefused() =>
        new Sut().Validate(new UpdateScheduleRequest { Name = "nightly-load", PipelineName = "Load", CronExpression = "0 3 * * *" }).Errors
            .ShouldContain(e => e.PropertyName == "PipelineName");

    [Fact]
    public void AnIntervalOverADayIsRefused() =>
        new Sut().Validate(new UpdateScheduleRequest { Name = "nightly-load", IntervalSeconds = 90000 }).Errors
            .ShouldContain(e => e.ErrorMessage == "Interval seconds cannot exceed 86400 (24 hours)");

    [Fact]
    public void ARunRoleListThatNamesNoneIsRefused() =>
        new Sut().Validate(new UpdateScheduleRequest { Name = "nightly-load", RunRoles = [] }).Errors
            .ShouldContain(e => e.PropertyName == "RunRoles");

    [Fact]
    public void ASchedulerTypeThatIsNotATriggerTypeIsRefused() =>
        new Sut().Validate(new UpdateScheduleRequest { Name = "nightly-load", SchedulerType = "Sometimes" }).Errors
            .ShouldContain(e => e.PropertyName == "SchedulerType");
}
