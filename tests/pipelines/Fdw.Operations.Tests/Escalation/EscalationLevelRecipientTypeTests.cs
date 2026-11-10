using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Configuration;
using Fdw.Operations.Escalation;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Tests.Escalation;

/// <summary>
/// Tests for the recipients of an escalation level (FDW-4891). The type is each recipient's own: a level with no recipients reads back an empty
/// list, a level whose recipients hold two types reads back both, and neither throws. The type is stored on each recipient row.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "Escalation")]
public sealed class EscalationLevelRecipientTypeTests
{
    private sealed class TestRecipient(string recipient, string recipientType) : IEscalationRecipient
    {
        public string Recipient { get; } = recipient;

        public string RecipientType { get; } = recipientType;
    }

    private sealed class TestLevel(params IEscalationRecipient[] recipients) : IEscalationLevel
    {
        public int Level => 1;

        public string Name => "First page";

        public int DelayMinutes => 0;

        public string NotificationChannel => "Console";

        public IReadOnlyList<IEscalationRecipient> Recipients { get; } = recipients;

        public string? MessageTemplate => null;

        public string Severity => "Error";
    }

    private sealed class TestPolicy(IEscalationLevel level) : IEscalationPolicy
    {
        public Guid Id => Guid.Empty;

        public string Name => "Alerts";

        public bool IsEnabled => false;

        public Guid? WorkflowId => null;

        public Guid? ScheduleId => Guid.Parse("01a1c408-9853-79c1-8ad2-355676f6ccb0");

        public int MaxEscalationLevel => 1;

        public int CooldownMinutes => 15;

        public int MaxRetries => 2;

        public int RetryDelaySeconds => 60;

        public IReadOnlyList<IEscalationLevel> Levels { get; } = [level];
    }

    private static EscalationLevelConfiguration Stored(params (string Recipient, string RecipientType)[] recipients) => new()
    {
        Level = 1,
        Name = "First page",
        NotificationChannel = "Console",
        Severity = "Error",
        Recipients = [.. recipients.Select(r => new EscalationLevelRecipientConfiguration { Name = r.Recipient, Recipient = r.Recipient, RecipientType = r.RecipientType })]
    };

    [Fact]
    public void ALevelWithNoRecipientsReadsBackAnEmptyList()
    {
        new EscalationLevelRecord(Stored()).Recipients.ShouldBeEmpty();
    }

    [Fact]
    public void ALevelWithOneTypeReadsBackItsRecipientsWithThatType()
    {
        var recipients = new EscalationLevelRecord(Stored(("a@example.test", "Email"), ("b@example.test", "Email"))).Recipients;

        recipients.Select(r => (r.Recipient, r.RecipientType)).ShouldBe([("a@example.test", "Email"), ("b@example.test", "Email")]);
    }

    [Fact]
    public void ALevelWithTwoTypesReadsBackEachRecipientWithItsOwnTypeAndDoesNotThrow()
    {
        var recipients = new EscalationLevelRecord(Stored(("a@example.test", "Email"), ("https://hooks.example.test/x", "Webhook"))).Recipients;

        recipients.Select(r => (r.Recipient, r.RecipientType)).ShouldBe([("a@example.test", "Email"), ("https://hooks.example.test/x", "Webhook")]);
    }

    [Fact]
    public void EachRecipientIsStoredWithItsOwnType()
    {
        var level = new TestLevel(new TestRecipient("a@example.test", "Email"), new TestRecipient("key-1", "IntegrationKey"));

        var stored = EscalationService.BuildConfig(new TestPolicy(level), id: null).Levels.Single().Recipients;

        stored.Select(r => (r.Recipient, r.RecipientType)).ShouldBe([("a@example.test", "Email"), ("key-1", "IntegrationKey")]);
    }

    [Fact]
    public void ALevelWithNoRecipientsIsStoredWithNoRecipientRows()
    {
        EscalationService.BuildConfig(new TestPolicy(new TestLevel()), id: null).Levels.Single().Recipients.ShouldBeEmpty();
    }
}
