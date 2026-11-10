using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Data;
using Fdw.Operations.Escalation;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Operations.Tests.Escalation;

/// <summary>
/// Tests for the alerts escalation policies walk (FDW-3863): a threshold that fires opens one alert on the policy it names, a second firing opens nothing while it is open, a
/// resolved one cools down, a policy that cannot be used is a failure naming it, a person takes or closes an alert only when its state allows, and a level whose priority setting
/// is missing is not sent and not recorded as sent.
/// </summary>
public sealed class EscalationAlertsTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Start = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class MemoryStore : IEscalationAlertStore
    {
        public List<EscalationAlertRow> Rows { get; } = [];

        public Task<IGenericResult> Add(EscalationAlertRow alert, CancellationToken cancellationToken = default)
        {
            Rows.Add(alert);
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }

        public Task<IGenericResult> Update(EscalationAlertRow alert, CancellationToken cancellationToken = default) => Task.FromResult<IGenericResult>(GenericResult.Success());

        public Task<IGenericResult<EscalationAlertRow?>> Find(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<IGenericResult<EscalationAlertRow?>>(GenericResult<EscalationAlertRow?>.Success(Rows.FirstOrDefault(row => row.Id == id)));

        public Task<IGenericResult<IReadOnlyList<EscalationAlertRow>>> OfThreshold(
            Guid tenantId, string subjectKind, string subjectName, string thresholdName, CancellationToken cancellationToken = default) =>
            Task.FromResult<IGenericResult<IReadOnlyList<EscalationAlertRow>>>(GenericResult<IReadOnlyList<EscalationAlertRow>>.Success(
                [.. Rows.Where(row => row.TenantId == tenantId && row.SubjectKind == subjectKind && row.SubjectName == subjectName && row.ThresholdName == thresholdName)]));

        public Task<IGenericResult<IReadOnlyList<EscalationAlertRow>>> List(string? state, CancellationToken cancellationToken = default) =>
            Task.FromResult<IGenericResult<IReadOnlyList<EscalationAlertRow>>>(GenericResult<IReadOnlyList<EscalationAlertRow>>.Success(
                [.. Rows.Where(row => state is null || row.State == state)]));
    }

    private sealed class TestRecipient(string recipient, string recipientType) : IEscalationRecipient
    {
        public string Recipient { get; } = recipient;

        public string RecipientType { get; } = recipientType;
    }

    private sealed class TestLevel(int level, int delayMinutes) : IEscalationLevel
    {
        public int Level { get; } = level;

        public string Name => "Level";

        public int DelayMinutes { get; } = delayMinutes;

        public string NotificationChannel => "OpsMail";

        public IReadOnlyList<IEscalationRecipient> Recipients => [new TestRecipient("ops@example.test", "Email")];

        public string? MessageTemplate => null;

        public string Severity => "Warning";
    }

    private sealed class TestPolicy(string name, bool enabled, int max, int cooldownMinutes, params IEscalationLevel[] levels) : IEscalationPolicy
    {
        public Guid Id { get; } = Guid.NewGuid();

        public string Name { get; } = name;

        public bool IsEnabled { get; } = enabled;

        public Guid? WorkflowId => null;
        public Guid? ScheduleId => null;

        public int MaxEscalationLevel { get; } = max;

        public int CooldownMinutes { get; } = cooldownMinutes;

        public int MaxRetries => 0;

        public int RetryDelaySeconds => 0;

        public IReadOnlyList<IEscalationLevel> Levels { get; } = levels;
    }

    private sealed class Settings(string? priority) : IEffectiveSettingsProvider
    {
        public T GetEffectiveValue<T>(string settingName, Guid? tenantId = null, string? roleName = null) => throw new NotSupportedException();

        public bool TryGetEffectiveValue<T>(string settingName, out T value, Guid? tenantId = null, string? roleName = null)
        {
            if (priority is not null && settingName == SettingDefinitions.EscalationNotificationPriority && typeof(T) == typeof(string))
            {
                value = (T)(object)priority;
                return true;
            }

            value = default!;
            return false;
        }
    }

    private readonly MemoryStore _store = new();
    private readonly Clock _clock = new(Start);
    private readonly Mock<IEscalationService> _policies = new();
    private readonly Mock<IAuthenticationContextAccessor> _auth = new();

    private EscalationAlerts Sut(string? priority = null, params IEscalationPolicy[] policies)
    {
        _policies
            .Setup(p => p.GetAllPolicies(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IEscalationPolicy>>.Success(policies));
        _policies
            .Setup(p => p.GetEnabledPolicies(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IEscalationPolicy>>.Success(policies.Where(p => p.IsEnabled).ToList()));
        var context = new Mock<IAuthenticationContext>();
        context.SetupGet(c => c.IsSystemContext).Returns(false);
        context.SetupGet(c => c.ActiveTenantId).Returns(Tenant);
        context.SetupGet(c => c.Claims).Returns(new Dictionary<string, object>());
        _auth.SetupGet(a => a.Current).Returns(context.Object);
        return new EscalationAlerts(
            _store, _policies.Object, new Mock<INotificationServiceProvider>().Object, new Settings(priority), _auth.Object, _clock, NullLogger.Instance);
    }

    private static TestPolicy Ops(bool enabled = true, int cooldown = 30) => new("Ops", enabled, 2, cooldown, new TestLevel(1, 5), new TestLevel(2, 15));

    private static EscalationAlertRequest Fired(string policy = "Ops") =>
        new(Tenant, "Pipeline", "monthly-sales", "FailedRuns", policy, "monthly-sales keeps failing", "Last failures: ...");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AThresholdThatFiresOpensOneAlertOnThePolicyItNames()
    {
        var opened = await Sut(null, Ops()).Open(Fired(), TestContext.Current.CancellationToken);

        opened.IsSuccess.ShouldBeTrue(opened.CurrentMessage);
        opened.Value!.Outcome.ShouldBe(EscalationOpenOutcomes.Opened);
        var alert = opened.Value.Alert.ShouldNotBeNull();
        alert.State.ShouldBe(EscalationAlertStates.Firing);
        alert.CurrentLevel.ShouldBe(0);
        alert.PolicyName.ShouldBe("Ops");
        alert.FiredAt.ShouldBe(Start);
        _store.Rows.ShouldHaveSingleItem();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AThresholdThatFiresAgainWhileItsAlertIsOpenOpensNothingNew()
    {
        var sut = Sut(null, Ops());
        var first = await sut.Open(Fired(), TestContext.Current.CancellationToken);

        var again = await sut.Open(Fired(), TestContext.Current.CancellationToken);

        again.Value!.Outcome.ShouldBe(EscalationOpenOutcomes.AlreadyOpen);
        again.Value.Alert!.Id.ShouldBe(first.Value!.Alert!.Id);
        _store.Rows.ShouldHaveSingleItem();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AThresholdResolvedWithinThePoliciesCooldownOpensNothingAndAfterItOpensANewAlert()
    {
        var sut = Sut(null, Ops(cooldown: 30));
        var first = await sut.Open(Fired(), TestContext.Current.CancellationToken);
        (await sut.Resolve(first.Value!.Alert!.Id, "mike", TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _clock.Now = Start.AddMinutes(29);
        (await sut.Open(Fired(), TestContext.Current.CancellationToken)).Value!.Outcome.ShouldBe(EscalationOpenOutcomes.CoolingDown);

        _clock.Now = Start.AddMinutes(30);
        (await sut.Open(Fired(), TestContext.Current.CancellationToken)).Value!.Outcome.ShouldBe(EscalationOpenOutcomes.Opened);
        _store.Rows.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APolicyThatIsMissingNotEnabledOrWithoutALevelIsAFailureAndOpensNothing()
    {
        (await Sut(null, Ops()).Open(Fired("Nobody"), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        (await Sut(null, Ops(enabled: false)).Open(Fired(), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        (await Sut(null, new TestPolicy("Ops", true, 2, 30)).Open(Fired(), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        _store.Rows.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APersonAcknowledgesAFiringAlertOnceAndResolvesAnOpenOneOnce()
    {
        var sut = Sut(null, Ops());
        var alert = (await sut.Open(Fired(), TestContext.Current.CancellationToken)).Value!.Alert!;

        var acknowledged = await sut.Acknowledge(alert.Id, "mike", TestContext.Current.CancellationToken);
        acknowledged.Value!.State.ShouldBe(EscalationAlertStates.Acknowledged);
        acknowledged.Value.AcknowledgedBy.ShouldBe("mike");
        (await sut.Acknowledge(alert.Id, "mike", TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();

        var resolved = await sut.Resolve(alert.Id, "mike", TestContext.Current.CancellationToken);
        resolved.Value!.State.ShouldBe(EscalationAlertStates.Resolved);
        resolved.Value.ResolvedAt.ShouldBe(Start);
        (await sut.Resolve(alert.Id, "mike", TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnAlertOfAnotherTenantAnswersAsOneThatDoesNotExist()
    {
        var sut = Sut(null, Ops());
        _store.Rows.Add(new EscalationAlertRow { TenantId = Guid.NewGuid(), State = EscalationAlertStates.Firing, PolicyName = "Ops", SubjectKind = "Pipeline", SubjectName = "x", ThresholdName = "t" });

        (await sut.Acknowledge(_store.Rows[0].Id, "mike", TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        (await sut.List(null, TestContext.Current.CancellationToken)).Value!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStateThatIsNotOneOfTheThreeIsRefusedNamingIt()
    {
        (await Sut(null, Ops()).List("Pending", TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ALevelIsNotSentAndNotRecordedWhenThePrioritySettingIsMissing()
    {
        var sut = Sut(null, Ops());
        var alert = (await sut.Open(Fired(), TestContext.Current.CancellationToken)).Value!.Alert!;

        var advanced = await sut.Advance(Start.AddMinutes(10), TestContext.Current.CancellationToken);

        advanced.Value.ShouldBe(0);
        _store.Rows.Single(row => row.Id == alert.Id).CurrentLevel.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task NothingIsSentBeforeTheFirstLevelsDelayHasPassed()
    {
        var sut = Sut("High", Ops());
        await sut.Open(Fired(), TestContext.Current.CancellationToken);

        (await sut.Advance(Start.AddMinutes(4), TestContext.Current.CancellationToken)).Value.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task EachReasonAPolicyCannotBeUsedHasItsOwnCodedRefusal()
    {
        var missing = await Sut(null, Ops()).Open(Fired("Nobody"), TestContext.Current.CancellationToken);
        var notEnabled = await Sut(null, Ops(enabled: false)).Open(Fired(), TestContext.Current.CancellationToken);
        var noLevel = await Sut(null, new TestPolicy("Ops", true, 2, 30)).Open(Fired(), TestContext.Current.CancellationToken);

        missing.Messages[^1].Code.ShouldBe("ESCALATIONALERT-31005");
        notEnabled.Messages[^1].Code.ShouldBe("ESCALATIONALERT-21007");
        noLevel.Messages[^1].Code.ShouldBe("ESCALATIONALERT-21009");
        _store.Rows.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnOpenAlertOnAPolicyThatWasSwitchedOffIsNotAdvancedAndThePassReadsOnlyEnabledPolicies()
    {
        var alert = (await Sut("High", Ops()).Open(Fired(), TestContext.Current.CancellationToken)).Value!.Alert!;
        var sut = Sut("High", Ops(enabled: false));

        var advanced = await sut.Advance(Start.AddMinutes(10), TestContext.Current.CancellationToken);

        advanced.Value.ShouldBe(0);
        _store.Rows.Single(row => row.Id == alert.Id).CurrentLevel.ShouldBe(0);
        _policies.Verify(p => p.GetEnabledPolicies(It.IsAny<CancellationToken>()), Times.Once);
    }
}
