using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Messaging.Abstractions;
using Fdw.Services.Messaging.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Messaging.Tests;

/// <summary>
/// FDW-2420: <see cref="AlertActionService"/>, the actions a person takes on an alert. The alerts come from stub sources, so these tests are about what the
/// service decides (what it refuses, what it offers, when an alert is hidden and when it returns), and the gateway is a fake that answers every read with the rows
/// it was seeded with and records which container each write touched.
/// </summary>
public sealed class AlertActionServiceTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid Ben = Guid.NewGuid();
    private static readonly Guid Ana = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);

    private sealed class StubGatewayProvider(IDataGateway gateway) : IDataGatewayProvider
    {
        public IDataGateway ByName(string name) => gateway;
    }

    private sealed class StubAccessor : IAuthenticationContextAccessor
    {
        public IAuthenticationContext? Current { get; set; }
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    /// <summary>A kind of alert that finds the alerts it was given and records what it was told.</summary>
    private sealed class StubSource(string kind, params string[] offered) : IAlertSource
    {
        public string Kind => kind;

        public IReadOnlyList<string> OfferedActions { get; } = offered;

        public List<AlertSubject> Alerts { get; } = [];

        public List<string> Told { get; } = [];

        public IGenericResult? TellFails { get; set; }

        public Task<IGenericResult<AlertSubject?>> Find(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult<AlertSubject?>>(GenericResult<AlertSubject?>.Success(Alerts.FirstOrDefault(a => a.Id == id)));

        public Task<IGenericResult> Acted(Guid id, IAlertAction action, CancellationToken cancellationToken = default)
        {
            Told.Add($"{id:D}:{action.Name}");
            return Task.FromResult(TellFails ?? GenericResult.Success());
        }

        public AlertSubject Add()
        {
            var alert = new AlertSubject(Guid.NewGuid(), TenantA);
            Alerts.Add(alert);
            return alert;
        }
    }

    private sealed class Fixture
    {
        public Fixture(DateTimeOffset? now = null, params StubSource[] sources)
        {
            Sources = sources.Length > 0 ? sources : new[] { new StubSource("Stub", "Acknowledge", "Snooze", "Silence", "Dismiss", "Discard") };
            var gateway = new Mock<IDataGateway>(MockBehavior.Loose);
            gateway
                .Setup(g => g.Execute<IEnumerable<AlertActionRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GenericResult<IEnumerable<AlertActionRow>>.Success(Mine.ToList()));
            gateway
                .Setup(g => g.Execute<IEnumerable<AlertEveryoneActionRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GenericResult<IEnumerable<AlertEveryoneActionRow>>.Success(Everyone.ToList()));
            gateway
                .Setup(g => g.Execute<IEnumerable<AlertKindSettingRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GenericResult<IEnumerable<AlertKindSettingRow>>.Success(KindSettings.ToList()));
            gateway
                .Setup(g => g.Execute<IEnumerable<AlertKindSettingActionRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GenericResult<IEnumerable<AlertKindSettingActionRow>>.Success(KindSettingActions.ToList()));
            gateway
                .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .Returns<IDataCommand, DataStoreTarget, CancellationToken>((_, target, _) =>
                {
                    Writes.Add(target.Container);
                    return Task.FromResult<IGenericResult<int>>(GenericResult<int>.Success(1));
                });

            var messaging = new Mock<IMessagingConfigurationProvider>(MockBehavior.Loose);
            messaging
                .Setup(m => m.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IMessagingImplementationConfiguration>.Success(
                    new MessagingImplementationConfiguration { Name = "Messaging", DataStoreName = "OpsDb", PathName = "msg" }));

            var names = new Mock<IUserNames>(MockBehavior.Loose);
            names
                .Setup(n => n.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, CancellationToken _) => AccountNames.TryGetValue(id, out var name)
                    ? GenericResult<string>.Success(name)
                    : GenericResult<string>.Failure(new GenericMessage("the signed-in person has no account")));

            Service = new AlertActionService(
                NullLogger<AlertActionService>.Instance, new StubGatewayProvider(gateway.Object), messaging.Object, Accessor, names.Object, new FixedClock(now ?? Now), Sources);
        }

        public AlertActionService Service { get; }

        public StubSource[] Sources { get; }

        public StubSource Source => Sources[0];

        public StubAccessor Accessor { get; } = new();

        /// <summary>The actions the signed-in person has taken on alerts; the fake answers every read with these.</summary>
        public List<AlertActionRow> Mine { get; } = [];

        /// <summary>The actions taken for everyone on alerts.</summary>
        public List<AlertEveryoneActionRow> Everyone { get; } = [];

        /// <summary>The kinds a tenant chose the actions of.</summary>
        public List<AlertKindSettingRow> KindSettings { get; } = [];

        /// <summary>The actions a tenant chose for those kinds.</summary>
        public List<AlertKindSettingActionRow> KindSettingActions { get; } = [];

        /// <summary>Says the company of the alerts chose these actions, in this order, for the stub kind.</summary>
        public void CompanyChose(params string[] actions)
        {
            KindSettings.Add(new AlertKindSettingRow { TenantId = TenantA, Kind = "Stub", ChangedByUserId = Ana, ChangedByName = "Ana", ChangedAt = Now });
            for (var i = 0; i < actions.Length; i++)
                KindSettingActions.Add(new AlertKindSettingActionRow { TenantId = TenantA, Kind = "Stub", Action = actions[i], Ordinal = i });
        }

        public Dictionary<Guid, string> AccountNames { get; } = [];

        /// <summary>The container each write touched, in order.</summary>
        public List<string> Writes { get; } = [];

        public Fixture SignedInAs(Guid userId, string name, params string[] permissions)
        {
            var context = new Mock<IAuthenticationContext>(MockBehavior.Loose);
            context.SetupGet(c => c.IsAuthenticated).Returns(true);
            context.SetupGet(c => c.UserId).Returns(userId.ToString("D"));
            context.SetupGet(c => c.Username).Returns(userId.ToString("D"));
            AccountNames[userId] = name;
            context.SetupGet(c => c.ActiveTenantId).Returns(TenantA);
            context.SetupGet(c => c.Permissions).Returns(permissions);
            Accessor.Current = context.Object;
            return this;
        }

        public AlertActionRow Took(AlertSubject alert, string action, DateTimeOffset? until = null, string kind = "Stub")
        {
            var row = new AlertActionRow { TenantId = TenantA, Kind = kind, AlertId = alert.Id, UserId = Ben, Action = action, UserName = "Ben", ActedAt = Now.AddMinutes(-5), Until = until };
            Mine.Add(row);
            return row;
        }

        public AlertEveryoneActionRow TookForEveryone(AlertSubject alert, string action, DateTimeOffset? until = null)
        {
            var row = new AlertEveryoneActionRow { TenantId = TenantA, Kind = "Stub", AlertId = alert.Id, Action = action, ActedByUserId = Ana, ActedByName = "Ana", ActedAt = Now.AddMinutes(-9), Until = until };
            Everyone.Add(row);
            return row;
        }
    }

    private static Fixture BenSignedIn(params string[] permissions) => new Fixture().SignedInAs(Ben, "Ben", permissions);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static AlertKey Key(AlertSubject alert) => new("Stub", alert.Id);

    // ── What an alert allows ────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AnAlertOffersTheActionsItsKindNamesInOrderAndHidesDiscardFromAPersonWithoutTheRight()
    {
        var fixture = BenSignedIn();
        var alert = fixture.Source.Add();

        var listed = await fixture.Service.List([Key(alert)], Ct);

        listed.IsSuccess.ShouldBeTrue();
        var item = listed.Value!.Items.ShouldHaveSingleItem();
        item.Shown.ShouldBeTrue();
        item.ReturnsAt.ShouldBeNull();
        item.Actions.Select(a => a.Name).ShouldBe(["Acknowledge", "Snooze", "Silence", "Dismiss"]);
        item.Actions.Single(a => a.Name == "Snooze").NeedsChoice.ShouldBeTrue();
        item.Actions.Single(a => a.Name == "Snooze").Choices.Select(c => c.Name).ShouldBe(["quarter-hour", "hour", "tomorrow"]);
        item.Actions.Single(a => a.Name == "Acknowledge").NeedsChoice.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task DiscardIsOfferedToAPersonWhoHoldsTheRight()
    {
        var fixture = BenSignedIn("alerts:discard");
        var alert = fixture.Source.Add();

        var listed = await fixture.Service.List([Key(alert)], Ct);

        var discard = listed.Value!.Items.Single().Actions.Single(a => a.Name == "Discard");
        discard.ForEveryone.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnAlertTheCallerMayNotSeeIsMissingAndNeverAnError()
    {
        var fixture = BenSignedIn();
        var seen = fixture.Source.Add();
        var hidden = new AlertSubject(Guid.NewGuid(), TenantA);

        var listed = await fixture.Service.List([Key(seen), Key(hidden)], Ct);

        listed.IsSuccess.ShouldBeTrue();
        listed.Value!.Items.Select(i => i.Id).ShouldBe([seen.Id]);
        listed.Value.Missing.Select(m => m.Id).ShouldBe([hidden.Id]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task EachAlertIsAnsweredInTheOrderAskedAndOnlyOnce()
    {
        var fixture = BenSignedIn();
        var first = fixture.Source.Add();
        var second = fixture.Source.Add();

        var listed = await fixture.Service.List([Key(second), Key(first), Key(second)], Ct);

        listed.Value!.Items.Select(i => i.Id).ShouldBe([second.Id, first.Id]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AKindIsFoundWhateverTheCaseItIsWritten()
    {
        var fixture = BenSignedIn();
        var alert = fixture.Source.Add();

        var listed = await fixture.Service.List([new AlertKey("stub", alert.Id)], Ct);

        listed.Value!.Items.ShouldHaveSingleItem().Kind.ShouldBe("Stub");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ListRefusesAWronglyPutQuestion()
    {
        var fixture = BenSignedIn();
        var alert = fixture.Source.Add();

        var none = await fixture.Service.List([], Ct);
        var tooMany = await fixture.Service.List([.. Enumerable.Range(0, AlertLimits.MaxAlerts + 1).Select(_ => Key(alert))], Ct);
        var unknown = await fixture.Service.List([new AlertKey("Nothing", alert.Id)], Ct);

        none.Messages.ShouldContain(m => m.Code == "MESSAGING-21013");
        tooMany.Messages.ShouldContain(m => m.Code == "MESSAGING-21013");
        unknown.Messages.ShouldContain(m => m.Code == "MESSAGING-21010");
        unknown.CurrentMessage!.ShouldContain("Stub");
    }

    // ── When an alert is shown ──────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ASnoozedAlertIsHiddenUntilItsTimeAndThenReturnsByItself()
    {
        var fixture = BenSignedIn();
        var alert = fixture.Source.Add();
        fixture.Took(alert, "Snooze", Now.AddMinutes(15));

        var during = (await fixture.Service.List([Key(alert)], Ct)).Value!.Items.Single();

        during.Shown.ShouldBeFalse();
        during.ReturnsAt.ShouldBe(Now.AddMinutes(15));
        during.Taken.Single().Action.ShouldBe("Snooze");
        during.Actions.Single(a => a.Name == "Snooze").Done.ShouldBeTrue();

        var later = new Fixture(Now.AddMinutes(16), fixture.Source).SignedInAs(Ben, "Ben");
        later.Took(alert, "Snooze", Now.AddMinutes(15));

        var after = (await later.Service.List([Key(alert)], Ct)).Value!.Items.Single();

        after.Shown.ShouldBeTrue();
        after.ReturnsAt.ShouldBeNull();
        after.Taken.ShouldBeEmpty();
        after.Actions.Single(a => a.Name == "Snooze").Done.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ADismissedAlertIsHiddenForGoodButStillOffersTheCallerTheirOwnActions()
    {
        var fixture = BenSignedIn();
        var alert = fixture.Source.Add();
        fixture.Took(alert, "Dismiss");

        var item = (await fixture.Service.List([Key(alert)], Ct)).Value!.Items.Single();

        item.Shown.ShouldBeFalse();
        item.ReturnsAt.ShouldBeNull();
        item.Actions.Single(a => a.Name == "Dismiss").Done.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AnAcknowledgedAlertIsStillShown()
    {
        var fixture = BenSignedIn();
        var alert = fixture.Source.Add();
        fixture.Took(alert, "Acknowledge");

        var item = (await fixture.Service.List([Key(alert)], Ct)).Value!.Items.Single();

        item.Shown.ShouldBeTrue();
        item.Taken.Single().Action.ShouldBe("Acknowledge");
        item.Actions.Single(a => a.Name == "Acknowledge").Done.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AnAlertReturnsWhenTheLatestOfTheActionsHidingItIsOver()
    {
        var fixture = BenSignedIn();
        var alert = fixture.Source.Add();
        fixture.Took(alert, "Snooze", Now.AddHours(1));
        fixture.Took(alert, "Silence", Now.AddDays(1));

        var item = (await fixture.Service.List([Key(alert)], Ct)).Value!.Items.Single();

        item.Shown.ShouldBeFalse();
        item.ReturnsAt.ShouldBe(Now.AddDays(1));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AnActionWithNoEndHidingTheAlertMeansItDoesNotReturn()
    {
        var fixture = BenSignedIn();
        var alert = fixture.Source.Add();
        fixture.Took(alert, "Snooze", Now.AddHours(1));
        fixture.Took(alert, "Dismiss");

        var item = (await fixture.Service.List([Key(alert)], Ct)).Value!.Items.Single();

        item.Shown.ShouldBeFalse();
        item.ReturnsAt.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ADiscardedAlertIsGoneForEveryoneAndOffersNothingButSaysWhoDiscardedIt()
    {
        var fixture = BenSignedIn("alerts:discard");
        var alert = fixture.Source.Add();
        fixture.TookForEveryone(alert, "Discard");

        var item = (await fixture.Service.List([Key(alert)], Ct)).Value!.Items.Single();

        item.Shown.ShouldBeFalse();
        item.Actions.ShouldBeEmpty();
        var taken = item.Taken.ShouldHaveSingleItem();
        taken.ForEveryone.ShouldBeTrue();
        taken.ByName.ShouldBe("Ana");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStoredActionThatIsNoLongerDeclaredFailsLoudly()
    {
        var fixture = BenSignedIn();
        var alert = fixture.Source.Add();
        fixture.Took(alert, "Retired");

        var listed = await fixture.Service.List([Key(alert)], Ct);

        listed.IsFailure.ShouldBeTrue();
        listed.Messages.ShouldContain(m => m.Code == "MESSAGING-91010");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AKindThatOffersAnActionThatIsNotDeclaredFailsLoudly()
    {
        var fixture = new Fixture(null, new StubSource("Stub", "Acknowledge", "Retired")).SignedInAs(Ben, "Ben");
        var alert = fixture.Source.Add();

        var listed = await fixture.Service.List([Key(alert)], Ct);

        listed.IsFailure.ShouldBeTrue();
        listed.Messages.ShouldContain(m => m.Code == "MESSAGING-91010");
    }

    // ── Taking an action ────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AcknowledgeIsKeptForThePersonAndTheKindIsTold()
    {
        var fixture = BenSignedIn();
        var alert = fixture.Source.Add();

        var applied = await fixture.Service.Apply("Stub", alert.Id, "Acknowledge", null, Ct);

        applied.IsSuccess.ShouldBeTrue();
        fixture.Writes.ShouldBe(["AlertAction", "AlertAction"]);
        fixture.Source.Told.ShouldBe([$"{alert.Id:D}:Acknowledge"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AcknowledgingAgainChangesNothingButStillTellsTheKind()
    {
        var fixture = BenSignedIn();
        var alert = fixture.Source.Add();
        fixture.Took(alert, "Acknowledge");

        var applied = await fixture.Service.Apply("Stub", alert.Id, "Acknowledge", null, Ct);

        applied.IsSuccess.ShouldBeTrue();
        fixture.Writes.ShouldBeEmpty();
        fixture.Source.Told.ShouldBe([$"{alert.Id:D}:Acknowledge"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ASnoozeTakenAgainMovesItsEnd()
    {
        var fixture = BenSignedIn();
        var alert = fixture.Source.Add();
        fixture.Took(alert, "Snooze", Now.AddMinutes(10));

        var applied = await fixture.Service.Apply("Stub", alert.Id, "Snooze", "hour", Ct);

        applied.IsSuccess.ShouldBeTrue();
        fixture.Writes.ShouldBe(["AlertAction", "AlertAction"]);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Snooze", null)]
    [InlineData("Snooze", " ")]
    [InlineData("Snooze", "next-year")]
    [InlineData("Acknowledge", "hour")]
    public async Task AChoiceIsRequiredOnlyWhereTheActionOffersLengthsAndMustBeOneOfThem(string action, string? choice)
    {
        var fixture = BenSignedIn();
        var alert = fixture.Source.Add();

        var applied = await fixture.Service.Apply("Stub", alert.Id, action, choice, Ct);

        applied.IsFailure.ShouldBeTrue();
        applied.Messages.ShouldContain(m => m.Code == "MESSAGING-21012");
        applied.CurrentMessage!.ShouldContain("choice");
        fixture.Writes.ShouldBeEmpty();
        fixture.Source.Told.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnUnknownActionOrKindIsRefusedByNamingItAndWritesNothing()
    {
        var fixture = BenSignedIn();
        var alert = fixture.Source.Add();

        var action = await fixture.Service.Apply("Stub", alert.Id, "Explode", null, Ct);
        var kind = await fixture.Service.Apply("Nothing", alert.Id, "Acknowledge", null, Ct);

        action.Messages.ShouldContain(m => m.Code == "MESSAGING-21011");
        action.CurrentMessage!.ShouldContain("Acknowledge");
        kind.Messages.ShouldContain(m => m.Code == "MESSAGING-21010");
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnAlertTheCallerMayNotSeeCannotBeActedOn()
    {
        var fixture = BenSignedIn();

        var applied = await fixture.Service.Apply("Stub", Guid.NewGuid(), "Acknowledge", null, Ct);

        applied.Messages.ShouldContain(m => m.Code == "MESSAGING-31010");
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AnActionTheKindDoesNotOfferIsAConflict()
    {
        var fixture = new Fixture(null, new StubSource("Stub", "Acknowledge")).SignedInAs(Ben, "Ben");
        var alert = fixture.Source.Add();

        var applied = await fixture.Service.Apply("Stub", alert.Id, "Dismiss", null, Ct);

        applied.Messages.ShouldContain(m => m.Code == "MESSAGING-41010");
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task DiscardNeedsTheRightAndWithoutItNothingIsWritten()
    {
        var fixture = BenSignedIn("ui-feedback:read");
        var alert = fixture.Source.Add();

        var applied = await fixture.Service.Apply("Stub", alert.Id, "Discard", null, Ct);

        applied.IsFailure.ShouldBeTrue();
        applied.Messages.ShouldContain(m => m.Code == "MESSAGING-100001");
        applied.CurrentMessage!.ShouldContain("alerts:discard");
        fixture.Writes.ShouldBeEmpty();
        fixture.Source.Told.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task DiscardByAPersonWithTheRightIsKeptOnceForEveryone()
    {
        var fixture = BenSignedIn("alerts:discard");
        var alert = fixture.Source.Add();

        var applied = await fixture.Service.Apply("Stub", alert.Id, "Discard", null, Ct);

        applied.IsSuccess.ShouldBeTrue();
        fixture.Writes.ShouldBe(["AlertEveryoneAction", "AlertEveryoneAction"]);
        fixture.Source.Told.ShouldBe([$"{alert.Id:D}:Discard"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task DiscardingAnAlreadyDiscardedAlertChangesNothing()
    {
        var fixture = BenSignedIn("alerts:discard");
        var alert = fixture.Source.Add();
        fixture.TookForEveryone(alert, "Discard");

        var applied = await fixture.Service.Apply("Stub", alert.Id, "Discard", null, Ct);

        applied.IsSuccess.ShouldBeTrue();
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ANamedPermissionIsCoveredByAHeldWildcard()
    {
        var fixture = BenSignedIn("*:*");
        var alert = fixture.Source.Add();

        var applied = await fixture.Service.Apply("Stub", alert.Id, "Discard", null, Ct);

        applied.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task APersonWithNoAccountCannotActBecauseWhoActedIsRecorded()
    {
        var fixture = new Fixture();
        fixture.SignedInAs(Ben, "Ben");
        fixture.AccountNames.Clear();
        var alert = fixture.Source.Add();

        var applied = await fixture.Service.Apply("Stub", alert.Id, "Acknowledge", null, Ct);

        applied.IsFailure.ShouldBeTrue();
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AFailureOfTheKindToRespondIsReportedAfterTheActionIsKept()
    {
        var fixture = BenSignedIn();
        var alert = fixture.Source.Add();
        fixture.Source.TellFails = GenericResult.Failure(new GenericMessage("the notice could not be marked read"));

        var applied = await fixture.Service.Apply("Stub", alert.Id, "Acknowledge", null, Ct);

        applied.IsFailure.ShouldBeTrue();
        fixture.Writes.ShouldBe(["AlertAction", "AlertAction"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task NobodySignedInCannotListOrActOnAlerts()
    {
        var fixture = new Fixture();
        var alert = fixture.Source.Add();

        var listed = await fixture.Service.List([Key(alert)], Ct);
        var applied = await fixture.Service.Apply("Stub", alert.Id, "Acknowledge", null, Ct);

        listed.Messages.ShouldContain(m => m.Code == "MESSAGING-51010");
        applied.Messages.ShouldContain(m => m.Code == "MESSAGING-51010");
        fixture.Writes.ShouldBeEmpty();
    }

    // ── Taking an action back ───────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task APersonTakesBackTheirOwnActionAndTheKindIsNotTold()
    {
        var fixture = BenSignedIn();
        var alert = fixture.Source.Add();
        fixture.Took(alert, "Dismiss");

        var taken = await fixture.Service.TakeBack("Stub", alert.Id, "Dismiss", Ct);

        taken.IsSuccess.ShouldBeTrue();
        fixture.Writes.ShouldBe(["AlertAction"]);
        fixture.Source.Told.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnActionTakenForEveryoneCannotBeTakenBackByOnePerson()
    {
        var fixture = BenSignedIn("alerts:discard");
        var alert = fixture.Source.Add();
        fixture.TookForEveryone(alert, "Discard");

        var taken = await fixture.Service.TakeBack("Stub", alert.Id, "Discard", Ct);

        taken.IsFailure.ShouldBeTrue();
        taken.Messages.ShouldContain(m => m.Code == "MESSAGING-41011");
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TakingBackAnActionOnAnAlertOutOfReachIsNotFound()
    {
        var fixture = BenSignedIn();

        var taken = await fixture.Service.TakeBack("Stub", Guid.NewGuid(), "Dismiss", Ct);

        taken.Messages.ShouldContain(m => m.Code == "MESSAGING-31010");
        fixture.Writes.ShouldBeEmpty();
    }

    // ── What a company chose (FDW-2430) ─────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ACompanysChosenActionsReplaceTheKindsOwnListInTheOrderItChoseThem()
    {
        var fixture = BenSignedIn("alerts:discard");
        var alert = fixture.Source.Add();
        fixture.CompanyChose("Dismiss", "Acknowledge");

        var item = (await fixture.Service.List([Key(alert)], Ct)).Value!.Items.Single();

        item.Actions.Select(a => a.Name).ShouldBe(["Dismiss", "Acknowledge"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ACompanyThatChoseNoActionsOffersNone()
    {
        var fixture = BenSignedIn("alerts:discard");
        var alert = fixture.Source.Add();
        fixture.CompanyChose();

        var item = (await fixture.Service.List([Key(alert)], Ct)).Value!.Items.Single();

        item.Shown.ShouldBeTrue();
        item.Actions.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AnActionTheCompanyDidNotChooseIsAConflictAndOneItDidChooseIsTaken()
    {
        var fixture = BenSignedIn();
        var alert = fixture.Source.Add();
        fixture.CompanyChose("Dismiss");

        var refused = await fixture.Service.Apply("Stub", alert.Id, "Snooze", "hour", Ct);
        var writesAfterRefusal = fixture.Writes.Count;
        var taken = await fixture.Service.Apply("Stub", alert.Id, "Dismiss", null, Ct);

        refused.IsFailure.ShouldBeTrue();
        refused.Messages.ShouldContain(m => m.Code == "MESSAGING-41010");
        writesAfterRefusal.ShouldBe(0);
        taken.IsSuccess.ShouldBeTrue();
        fixture.Source.Told.ShouldBe([$"{alert.Id:D}:Dismiss"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AnActionTheKindDoesNotOfferByDefaultCanBeOfferedWhenTheCompanyChoosesIt()
    {
        var fixture = new Fixture(null, new StubSource("Stub", "Acknowledge")).SignedInAs(Ben, "Ben");
        var alert = fixture.Source.Add();
        fixture.CompanyChose("Acknowledge", "Dismiss");

        var item = (await fixture.Service.List([Key(alert)], Ct)).Value!.Items.Single();
        var taken = await fixture.Service.Apply("Stub", alert.Id, "Dismiss", null, Ct);

        item.Actions.Select(a => a.Name).ShouldBe(["Acknowledge", "Dismiss"]);
        taken.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AChosenActionThatIsNoLongerDeclaredFailsLoudly()
    {
        var fixture = BenSignedIn();
        var alert = fixture.Source.Add();
        fixture.CompanyChose("Retired");

        var listed = await fixture.Service.List([Key(alert)], Ct);

        listed.IsFailure.ShouldBeTrue();
        listed.Messages.ShouldContain(m => m.Code == "MESSAGING-91010");
    }

    // ── The kinds ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TwoSourcesForOneKindAreRefusedWhenTheServiceIsMade()
    {
        Should.Throw<ArgumentException>(() => { _ = new Fixture(null, new StubSource("Stub", "Acknowledge"), new StubSource("stub", "Acknowledge")); });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task SeveralKindsAreAskedAboutInOneQuestion()
    {
        var comments = new StubSource("Comment", "Acknowledge");
        var failures = new StubSource("Failure", "Dismiss");
        var fixture = new Fixture(null, comments, failures).SignedInAs(Ben, "Ben");
        var comment = comments.Add();
        var failure = failures.Add();

        var listed = await fixture.Service.List([new AlertKey("Comment", comment.Id), new AlertKey("Failure", failure.Id)], Ct);

        listed.Value!.Items.Select(i => (i.Kind, i.Id)).ShouldBe([("Comment", comment.Id), ("Failure", failure.Id)]);
        listed.Value.Items[1].Actions.Select(a => a.Name).ShouldBe(["Dismiss"]);
    }
}
