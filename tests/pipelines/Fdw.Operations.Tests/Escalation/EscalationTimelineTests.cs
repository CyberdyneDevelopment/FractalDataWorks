using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Escalation;

namespace Fdw.Operations.Tests.Escalation;

/// <summary>
/// Tests for the timing rules of an escalation policy (FDW-3863): a level is due when the previous level's time plus its own delay has passed, levels go in order and stop at
/// the policy's maximum, a policy that is not enabled has none due, and a cooldown keeps a resolved threshold from alerting again at once.
/// </summary>
public sealed class EscalationTimelineTests
{
    private static readonly DateTimeOffset Fired = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    private sealed class TestLevel(int level, int delayMinutes) : IEscalationLevel
    {
        public int Level { get; } = level;

        public int DelayMinutes { get; } = delayMinutes;

        public string NotificationChannel => "Email";

        public IReadOnlyList<string> Recipients => ["ops"];

        public string? MessageTemplate => null;

        public string Severity => "Warning";

        public string RecipientType => "Email";
    }

    private sealed class TestPolicy(int max, int cooldownMinutes, bool enabled, params IEscalationLevel[] levels) : IEscalationPolicy
    {
        public Guid Id { get; } = Guid.NewGuid();

        public string Name => "Ops";

        public bool IsEnabled { get; } = enabled;

        public Guid? WorkflowId => null;

        public int MaxEscalationLevel { get; } = max;

        public int CooldownMinutes { get; } = cooldownMinutes;

        public int MaxRetries => 0;

        public int RetryDelaySeconds => 0;

        public IReadOnlyList<IEscalationLevel> Levels { get; } = levels;
    }

    private static TestPolicy TwoLevels(int max = 2, int cooldown = 30, bool enabled = true) =>
        new(max, cooldown, enabled, new TestLevel(2, 15), new TestLevel(1, 5));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheFirstLevelIsDueWhenItsDelayHasPassedSinceTheAlertFired()
    {
        var policy = TwoLevels();

        EscalationTimeline.Due(policy, 0, Fired, Fired.AddMinutes(4)).ShouldBeNull();
        EscalationTimeline.Due(policy, 0, Fired, Fired.AddMinutes(5)).ShouldNotBeNull().Level.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheNextLevelCountsItsDelayFromWhenThePreviousOneWasSent()
    {
        var policy = TwoLevels();
        var levelOneSentAt = Fired.AddMinutes(5);

        EscalationTimeline.Due(policy, 1, levelOneSentAt, levelOneSentAt.AddMinutes(14)).ShouldBeNull();
        EscalationTimeline.Due(policy, 1, levelOneSentAt, levelOneSentAt.AddMinutes(15)).ShouldNotBeNull().Level.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void NoLevelIsDuePastThePoliciesMaximumOrWhenNoneIsLeft()
    {
        EscalationTimeline.Due(TwoLevels(max: 1), 1, Fired, Fired.AddDays(1)).ShouldBeNull();
        EscalationTimeline.Due(TwoLevels(), 2, Fired, Fired.AddDays(1)).ShouldBeNull();
        EscalationTimeline.Next(TwoLevels(max: 1), 1).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void LevelsGoInTheOrderOfTheirNumbersWhateverOrderThePolicyListsThem()
    {
        EscalationTimeline.Next(TwoLevels(), 0).ShouldNotBeNull().Level.ShouldBe(1);
        EscalationTimeline.Next(TwoLevels(), 1).ShouldNotBeNull().Level.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void APolicyThatIsNotEnabledHasNoLevelDueAndOneWithNoLevelsHasNoNext()
    {
        EscalationTimeline.Due(TwoLevels(enabled: false), 0, Fired, Fired.AddDays(1)).ShouldBeNull();
        EscalationTimeline.Next(new TestPolicy(3, 10, true), 0).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ACooldownKeepsAResolvedThresholdFromAlertingAgainUntilItHasPassed()
    {
        var policy = TwoLevels(cooldown: 30);

        EscalationTimeline.CooldownPassed(policy, null, Fired).ShouldBeTrue();
        EscalationTimeline.CooldownPassed(policy, Fired, Fired.AddMinutes(29)).ShouldBeFalse();
        EscalationTimeline.CooldownPassed(policy, Fired, Fired.AddMinutes(30)).ShouldBeTrue();
    }
}
