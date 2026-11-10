using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Escalation;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Tests.Escalation;

/// <summary>
/// Tests for the configuration <see cref="EscalationService"/> stores for a policy (FDW-4852, FDW-4814): each level's Name, which the
/// level validator requires, and the policy's workflow or schedule scope reach the stored configuration.
/// </summary>
[Trait("Priority", "P0")]
public sealed class EscalationServiceBuildConfigTests
{
    private static readonly Guid ScheduleId = Guid.Parse("01a1c408-9853-79c1-8ad2-355676f6ccb0");
    private static readonly Guid WorkflowId = Guid.Parse("02b2d519-a964-7ad2-9be3-466787f7ddc1");

    private sealed class TestRecipient(string recipient, string recipientType) : IEscalationRecipient
    {
        public string Recipient { get; } = recipient;

        public string RecipientType { get; } = recipientType;
    }

    private sealed class TestLevel(int number, string name) : IEscalationLevel
    {
        public int Level { get; } = number;

        public string Name { get; } = name;

        public int DelayMinutes => 5;

        public string NotificationChannel => "Console";

        public IReadOnlyList<IEscalationRecipient> Recipients => [new TestRecipient("walk@example.test", "Email")];

        public string? MessageTemplate => null;

        public string Severity => "Critical";
    }

    private sealed class Policy(Guid? workflowId, Guid? scheduleId, params IEscalationLevel[] levels) : IEscalationPolicy
    {
        public Guid Id => Guid.Empty;

        public string Name => "Alerts";

        public bool IsEnabled => false;

        public Guid? WorkflowId { get; } = workflowId;

        public Guid? ScheduleId { get; } = scheduleId;

        public int MaxEscalationLevel => 2;

        public int CooldownMinutes => 15;

        public int MaxRetries => 2;

        public int RetryDelaySeconds => 60;

        public IReadOnlyList<IEscalationLevel> Levels { get; } = levels;
    }

    [Fact]
    public void EachLevelNameReachesTheStoredLevelConfiguration()
    {
        var config = EscalationService.BuildConfig(new Policy(null, ScheduleId, new TestLevel(1, "First page"), new TestLevel(2, "Escalate to the lead")), id: null);

        config.Levels.Select(l => l.Name).ShouldBe(new[] { "First page", "Escalate to the lead" });
    }

    [Fact]
    public void ASchedulePolicyStoresItsScheduleAndNoWorkflow()
    {
        var config = EscalationService.BuildConfig(new Policy(null, ScheduleId, new TestLevel(1, "First page")), id: null);

        config.ScheduleId.ShouldBe(ScheduleId);
        config.WorkflowId.ShouldBeNull();
    }

    [Fact]
    public void AWorkflowPolicyStoresItsWorkflowAndNoSchedule()
    {
        var config = EscalationService.BuildConfig(new Policy(WorkflowId, null, new TestLevel(1, "First page")), id: null);

        config.WorkflowId.ShouldBe(WorkflowId);
        config.ScheduleId.ShouldBeNull();
    }
}
