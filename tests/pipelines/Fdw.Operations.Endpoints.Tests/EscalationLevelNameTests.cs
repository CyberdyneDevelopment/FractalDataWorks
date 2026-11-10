using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Operations.Endpoints.Escalation;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests;

/// <summary>
/// Tests for the name an escalation level carries (FDW-4852). The configuration validator requires a level Name and nothing supplied
/// one, so no policy could be created. The caller now states it: it reaches the model and the response, and a level without one is
/// named in the refusal on create and on update.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "Api")]
public class EscalationLevelNameTests
{
    private static readonly Guid ScheduleId = Guid.Parse("01a1c408-9853-79c1-8ad2-355676f6ccb0");
    private static readonly Guid WorkflowId = Guid.Parse("02b2d519-a964-7ad2-9be3-466787f7ddc1");

    private static EscalationLevelResponse Level(int number, string name) => new()
    {
        Level = number,
        Name = name,
        DelayMinutes = number * 5,
        NotificationChannel = "Console",
        Recipients = [new EscalationRecipientResponse { Recipient = "walk@example.test", RecipientType = "Email" }],
        Severity = "Critical"
    };

    private static EscalationPolicyRequest Create(Guid? workflowId, Guid? scheduleId, params EscalationLevelResponse[] levels) => new()
    {
        Name = "Alerts",
        IsEnabled = false,
        WorkflowId = workflowId,
        ScheduleId = scheduleId,
        MaxEscalationLevel = 2,
        CooldownMinutes = 15,
        MaxRetries = 2,
        RetryDelaySeconds = 60,
        Levels = levels
    };

    [Fact]
    public void AScheduleScopedCreateWithLevelsKeepsEachLevelNameThroughTheModelToTheResponse()
    {
        var request = Create(null, ScheduleId, Level(1, "First page"), Level(2, "Escalate to the lead"));

        var response = EscalationMapper.ToDto(EscalationMapper.ToModel(request, true, 2, 15, 2, 60));

        EscalationMapper.Missing(request).ShouldBeEmpty();
        response.ScheduleId.ShouldBe(ScheduleId);
        response.Levels.Select(l => l.Name).ShouldBe(new[] { "First page", "Escalate to the lead" });
    }

    [Fact]
    public void AWorkflowScopedCreateWithLevelsKeepsEachLevelNameThroughTheModelToTheResponse()
    {
        var request = Create(WorkflowId, null, Level(1, "First page"));

        var response = EscalationMapper.ToDto(EscalationMapper.ToModel(request, true, 2, 15, 2, 60));

        EscalationMapper.Missing(request).ShouldBeEmpty();
        response.WorkflowId.ShouldBe(WorkflowId);
        response.Levels.Single().Name.ShouldBe("First page");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ACreateWhoseLevelHasNoNameIsRefusedNamingTheLevel(string name)
    {
        var request = Create(null, ScheduleId, Level(1, "First page"), Level(2, name));

        EscalationMapper.Missing(request).ShouldBe(new[] { "levels[2].name" });
    }

    [Fact]
    public void AnUpdateThatSendsALevelWithoutANameIsRefusedNamingTheLevel()
    {
        IReadOnlyList<EscalationLevelResponse> levels = [Level(1, ""), Level(2, "Second")];

        EscalationMapper.MissingOnLevels(levels).ShouldBe(new[] { "levels[1].name" });
    }

    [Fact]
    public void AnUpdateThatKeepsTheStoredLevelsKeepsTheirNames()
    {
        var existing = new EscalationPolicyModel
        {
            Id = Guid.CreateVersion7(),
            Name = "Alerts",
            ScheduleId = ScheduleId,
            Levels = [new EscalationLevelModel { Level = 1, Name = "First page", DelayMinutes = 5, NotificationChannel = "Console", Recipients = [new EscalationRecipientResponse { Recipient = "a", RecipientType = "Email" }], Severity = "Critical" }]
        };

        var model = EscalationMapper.ToModel(new UpdateEscalationPolicyRequest { Id = existing.Id, Name = "Renamed" }, existing);

        model.Levels.Single().Name.ShouldBe("First page");
    }
}
