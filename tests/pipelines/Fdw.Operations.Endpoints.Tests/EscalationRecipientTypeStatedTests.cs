using System;
using System.Linq;
using Fdw.Operations.Endpoints.Escalation;

namespace Fdw.Operations.Endpoints.Tests;

/// <summary>
/// Tests for the type each recipient of an escalation level states (FDW-4891). The type is the recipient's own, so a level with no recipients states
/// none, a recipient without one is named, and a level whose recipients hold two types reaches the model and the response with both.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "Api")]
public sealed class EscalationRecipientTypeStatedTests
{
    private static EscalationLevelResponse Level(params EscalationRecipientResponse[] recipients) => new()
    {
        Level = 1,
        Name = "First page",
        DelayMinutes = 0,
        NotificationChannel = "Console",
        Recipients = recipients,
        Severity = "Error"
    };

    [Fact]
    public void ALevelWithNoRecipientsStatesNoTypeAndIsNotMissingOne()
    {
        EscalationMapper.MissingOnLevels([Level()]).ShouldBeEmpty();
    }

    [Fact]
    public void ARecipientWithoutATypeIsNamedWithItsLevelAndPlace()
    {
        var missing = EscalationMapper.MissingOnLevels([Level(new EscalationRecipientResponse { Recipient = "a@example.test", RecipientType = "Email" }, new EscalationRecipientResponse { Recipient = "b@example.test" })]);

        missing.ShouldBe(["levels[1].recipients[1].recipientType"]);
    }

    [Fact]
    public void ALevelWithTwoTypesIsNotMissingAnyAndKeepsBothThroughTheModelToTheResponse()
    {
        var level = Level(new EscalationRecipientResponse { Recipient = "a@example.test", RecipientType = "Email" }, new EscalationRecipientResponse { Recipient = "https://hooks.example.test/x", RecipientType = "Webhook" });
        var request = new EscalationPolicyRequest
        {
            Name = "Alerts",
            IsEnabled = false,
            ScheduleId = Guid.Parse("01a1c408-9853-79c1-8ad2-355676f6ccb0"),
            MaxEscalationLevel = 1,
            CooldownMinutes = 15,
            MaxRetries = 2,
            RetryDelaySeconds = 60,
            Levels = [level]
        };

        EscalationMapper.Missing(request).ShouldBeEmpty();

        var model = EscalationMapper.ToModel(request, isEnabled: false, maxEscalationLevel: 1, cooldownMinutes: 15, maxRetries: 2, retryDelaySeconds: 60);
        var response = EscalationMapper.ToDto(model).Levels.Single();

        response.Recipients.Select(r => (r.Recipient, r.RecipientType)).ShouldBe([("a@example.test", "Email"), ("https://hooks.example.test/x", "Webhook")]);
    }
}
