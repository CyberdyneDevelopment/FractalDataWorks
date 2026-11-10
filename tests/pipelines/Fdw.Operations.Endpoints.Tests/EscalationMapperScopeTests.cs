using System;
using Fdw.Operations.Endpoints.Escalation;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests;

/// <summary>
/// Tests for the scope an escalation policy carries through <see cref="EscalationMapper"/> (FDW-4814): a policy is scoped to a
/// workflow or to a schedule, and both reach the model and the response. Before, only the workflow did, so a schedule-scoped
/// policy could not be created or read through the API.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Api")]
public class EscalationMapperScopeTests
{
    private static readonly Guid ScheduleId = Guid.Parse("01a1c408-9853-79c1-8ad2-355676f6ccb0");
    private static readonly Guid WorkflowId = Guid.Parse("02b2d519-a964-7ad2-9be3-466787f7ddc1");

    private static EscalationPolicyRequest Request(Guid? workflowId, Guid? scheduleId) => new()
    {
        Name = "Alerts",
        WorkflowId = workflowId,
        ScheduleId = scheduleId,
        Levels = []
    };

    private static EscalationPolicyModel Stored(Guid? workflowId, Guid? scheduleId) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = "Alerts",
        IsEnabled = true,
        WorkflowId = workflowId,
        ScheduleId = scheduleId,
        MaxEscalationLevel = 2,
        CooldownMinutes = 15,
        MaxRetries = 3,
        RetryDelaySeconds = 60
    };

    [Fact]
    public void ACreateScopedToAScheduleCarriesTheScheduleThroughTheModelToTheResponse()
    {
        var model = EscalationMapper.ToModel(Request(null, ScheduleId), true, 2, 15, 3, 60);
        var response = EscalationMapper.ToDto(model);

        model.ScheduleId.ShouldBe(ScheduleId);
        model.WorkflowId.ShouldBeNull();
        response.ScheduleId.ShouldBe(ScheduleId);
        response.WorkflowId.ShouldBeNull();
    }

    [Fact]
    public void ACreateScopedToAWorkflowCarriesTheWorkflowThroughTheModelToTheResponse()
    {
        var model = EscalationMapper.ToModel(Request(WorkflowId, null), true, 2, 15, 3, 60);
        var response = EscalationMapper.ToDto(model);

        model.WorkflowId.ShouldBe(WorkflowId);
        model.ScheduleId.ShouldBeNull();
        response.WorkflowId.ShouldBe(WorkflowId);
        response.ScheduleId.ShouldBeNull();
    }

    [Fact]
    public void AnUpdateThatNamesOnlyTheNameKeepsTheStoredScheduleScope()
    {
        var existing = Stored(null, ScheduleId);

        var model = EscalationMapper.ToModel(new UpdateEscalationPolicyRequest { Id = existing.Id, Name = "Renamed" }, existing);

        model.Name.ShouldBe("Renamed");
        model.ScheduleId.ShouldBe(ScheduleId);
        model.WorkflowId.ShouldBeNull();
    }

    [Fact]
    public void AnUpdateThatNamesOnlyTheNameKeepsTheStoredWorkflowScope()
    {
        var existing = Stored(WorkflowId, null);

        var model = EscalationMapper.ToModel(new UpdateEscalationPolicyRequest { Id = existing.Id, Name = "Renamed" }, existing);

        model.WorkflowId.ShouldBe(WorkflowId);
        model.ScheduleId.ShouldBeNull();
    }

    [Fact]
    public void AnUpdateThatNamesAScheduleMovesTheSchedulePolicyToTheNewOne()
    {
        var existing = Stored(null, Guid.Parse("03c3e62a-ba75-7be3-8cf4-577898080eed"));

        var model = EscalationMapper.ToModel(new UpdateEscalationPolicyRequest { Id = existing.Id, ScheduleId = ScheduleId }, existing);

        model.ScheduleId.ShouldBe(ScheduleId);
        model.WorkflowId.ShouldBeNull();
    }

    [Fact]
    public void AnUpdateThatNamesOnlyAScheduleMovesAWorkflowPolicyToTheScheduleAndClearsTheWorkflow()
    {
        var existing = Stored(WorkflowId, null);

        var model = EscalationMapper.ToModel(new UpdateEscalationPolicyRequest { Id = existing.Id, ScheduleId = ScheduleId }, existing);

        model.ScheduleId.ShouldBe(ScheduleId);
        model.WorkflowId.ShouldBeNull();
    }

    [Fact]
    public void AnUpdateThatNamesOnlyAWorkflowMovesASchedulePolicyToTheWorkflowAndClearsTheSchedule()
    {
        var existing = Stored(null, ScheduleId);

        var model = EscalationMapper.ToModel(new UpdateEscalationPolicyRequest { Id = existing.Id, WorkflowId = WorkflowId }, existing);

        model.WorkflowId.ShouldBe(WorkflowId);
        model.ScheduleId.ShouldBeNull();
    }

    [Fact]
    public void AnUpdateThatNamesBothScopesKeepsBothAsSent()
    {
        var existing = Stored(WorkflowId, null);

        var model = EscalationMapper.ToModel(new UpdateEscalationPolicyRequest { Id = existing.Id, WorkflowId = WorkflowId, ScheduleId = ScheduleId }, existing);

        model.WorkflowId.ShouldBe(WorkflowId);
        model.ScheduleId.ShouldBe(ScheduleId);
    }
}
