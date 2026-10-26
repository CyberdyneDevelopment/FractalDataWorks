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
/// FDW-2430: <see cref="AlertKindSettingsService"/>, which actions each kind of alert offers the people of a company. The kinds are stub sources and the
/// gateway is a fake that answers every read with the rows it was seeded with and records which container each write touched.
/// </summary>
public sealed class AlertKindSettingsServiceTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
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

    private sealed class StubSource(string kind, params string[] offered) : IAlertSource
    {
        public string Kind => kind;

        public IReadOnlyList<string> OfferedActions { get; } = offered;

        public Task<IGenericResult<AlertSubject?>> Find(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult<AlertSubject?>>(GenericResult<AlertSubject?>.Success(null));

        public Task<IGenericResult> Acted(Guid id, IAlertAction action, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult>(GenericResult.Success());
    }

    private sealed class Fixture
    {
        public Fixture()
        {
            var gateway = new Mock<IDataGateway>(MockBehavior.Loose);
            gateway
                .Setup(g => g.Execute<IEnumerable<AlertKindSettingRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GenericResult<IEnumerable<AlertKindSettingRow>>.Success(Settings.ToList()));
            gateway
                .Setup(g => g.Execute<IEnumerable<AlertKindSettingActionRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GenericResult<IEnumerable<AlertKindSettingActionRow>>.Success(SettingActions.ToList()));
            gateway
                .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .Returns<IDataCommand, DataStoreTarget, CancellationToken>((_, target, _) =>
                {
                    Writes.Add(target.Container);
                    return Task.FromResult<IGenericResult<int>>(
                        Writes.Count == FailWrite ? GenericResult<int>.Failure(Array.Empty<IGenericMessage>()) : GenericResult<int>.Success(1));
                });

            var messaging = new Mock<IMessagingConfigurationProvider>(MockBehavior.Loose);
            messaging
                .Setup(m => m.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IMessagingImplementationConfiguration>.Success(
                    new MessagingImplementationConfiguration { Name = "Messaging", DataStoreName = "OpsDb", PathName = "msg" }));

            var names = new Mock<IUserNames>(MockBehavior.Loose);
            names
                .Setup(n => n.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<string>.Success("Ana"));

            Service = new AlertKindSettingsService(
                NullLogger<AlertKindSettingsService>.Instance, new StubGatewayProvider(gateway.Object), messaging.Object, Accessor, names.Object, new FixedClock(Now),
                [new StubSource("Beta", "Dismiss"), new StubSource("Alpha", "Acknowledge", "Snooze")]);
        }

        public AlertKindSettingsService Service { get; }

        public StubAccessor Accessor { get; } = new();

        public List<AlertKindSettingRow> Settings { get; } = [];

        public List<AlertKindSettingActionRow> SettingActions { get; } = [];

        /// <summary>The container each write touched, in order.</summary>
        public List<string> Writes { get; } = [];

        /// <summary>The number (from 1) of the write that fails; 0 for none.</summary>
        public int FailWrite { get; set; }

        public Fixture SignedIn(Guid? tenantId = null)
        {
            var context = new Mock<IAuthenticationContext>(MockBehavior.Loose);
            context.SetupGet(c => c.IsAuthenticated).Returns(true);
            context.SetupGet(c => c.UserId).Returns(Ana.ToString("D"));
            context.SetupGet(c => c.ActiveTenantId).Returns(tenantId ?? TenantA);
            Accessor.Current = context.Object;
            return this;
        }

        public void CompanyChose(string kind, params string[] actions)
        {
            Settings.Add(new AlertKindSettingRow { TenantId = TenantA, Kind = kind, ChangedByUserId = Ana, ChangedByName = "Ana", ChangedAt = Now });
            for (var i = 0; i < actions.Length; i++)
                SettingActions.Add(new AlertKindSettingActionRow { TenantId = TenantA, Kind = kind, Action = actions[i], Ordinal = i });
        }
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task EveryKindIsListedByNameWithItsOwnActionsWhenTheCompanyChoseNone()
    {
        var fixture = new Fixture().SignedIn();

        var listed = await fixture.Service.List(Ct);

        listed.IsSuccess.ShouldBeTrue();
        listed.Value!.Select(k => k.Kind).ShouldBe(["Alpha", "Beta"]);
        var alpha = listed.Value[0];
        alpha.Configured.ShouldBeFalse();
        alpha.DefaultActions.ShouldBe(["Acknowledge", "Snooze"]);
        alpha.Actions.Select(a => a.Name).ShouldBe(["Acknowledge", "Snooze"]);
        alpha.Actions[0].DisplayName.ShouldBe("Acknowledge");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task EveryKindListsEveryDeclaredActionItCanBeGivenItsOwnFirstInItsOrder()
    {
        var fixture = new Fixture().SignedIn();
        fixture.CompanyChose("Alpha", "Dismiss");

        var listed = await fixture.Service.List(Ct);

        var alpha = listed.Value!.Single(k => k.Kind == "Alpha");
        alpha.Available.Select(a => a.Name).Take(2).ShouldBe(["Acknowledge", "Snooze"]);
        alpha.Available.Select(a => a.Name).OrderBy(n => n, StringComparer.Ordinal).ShouldBe(AlertActions.All().Select(a => a.Name).OrderBy(n => n, StringComparer.Ordinal));
        alpha.Available.Select(a => a.Name).Distinct().Count().ShouldBe(AlertActions.All().Count);
        alpha.Available.ShouldAllBe(a => a.DisplayName.Length > 0 && a.Description.Length > 0);
        listed.Value!.Single(k => k.Kind == "Beta").Available.Select(a => a.Name).First().ShouldBe("Dismiss");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task SavingAndResettingAnswerTheKindWithItsAvailableActions()
    {
        var fixture = new Fixture().SignedIn();

        var saved = await fixture.Service.Save("Alpha", ["Dismiss"], Ct);
        var reset = await fixture.Service.Reset("Alpha", Ct);

        saved.Value!.Available.Count.ShouldBe(AlertActions.All().Count);
        reset.Value!.Available.Count.ShouldBe(AlertActions.All().Count);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AKindTheCompanyConfiguredShowsItsChoiceInOrderAndTheOthersStayDefault()
    {
        var fixture = new Fixture().SignedIn();
        fixture.CompanyChose("Alpha", "Dismiss", "Acknowledge");

        var listed = await fixture.Service.List(Ct);

        var alpha = listed.Value!.Single(k => k.Kind == "Alpha");
        alpha.Configured.ShouldBeTrue();
        alpha.Actions.Select(a => a.Name).ShouldBe(["Dismiss", "Acknowledge"]);
        alpha.DefaultActions.ShouldBe(["Acknowledge", "Snooze"]);
        var beta = listed.Value!.Single(k => k.Kind == "Beta");
        beta.Configured.ShouldBeFalse();
        beta.Actions.Select(a => a.Name).ShouldBe(["Dismiss"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task SavingReplacesTheChoiceWholeAndWritesTheSettingThenItsActionsInOrder()
    {
        var fixture = new Fixture().SignedIn();

        var saved = await fixture.Service.Save("alpha", ["Snooze", "Dismiss"], Ct);

        saved.IsSuccess.ShouldBeTrue();
        saved.Value!.Kind.ShouldBe("Alpha");
        saved.Value.Configured.ShouldBeTrue();
        saved.Value.Actions.Select(a => a.Name).ShouldBe(["Snooze", "Dismiss"]);
        fixture.Writes.ShouldBe(["AlertKindSettingAction", "AlertKindSetting", "AlertKindSetting", "AlertKindSettingAction", "AlertKindSettingAction"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AnEmptyListIsAChoiceAndTheKindThenOffersNothing()
    {
        var fixture = new Fixture().SignedIn();

        var saved = await fixture.Service.Save("Alpha", [], Ct);

        saved.IsSuccess.ShouldBeTrue();
        saved.Value!.Configured.ShouldBeTrue();
        saved.Value.Actions.ShouldBeEmpty();
        fixture.Writes.ShouldBe(["AlertKindSettingAction", "AlertKindSetting", "AlertKindSetting"]);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Nothing", "Acknowledge", "MESSAGING-21010")]
    [InlineData("Alpha", "Explode", "MESSAGING-21011")]
    [InlineData("Alpha", "Dismiss,Dismiss", "MESSAGING-21014")]
    public async Task ASaveThatNamesAWrongKindOrActionOrRepeatsOneIsRefusedAndWritesNothing(string kind, string actions, string code)
    {
        var fixture = new Fixture().SignedIn();

        var saved = await fixture.Service.Save(kind, actions.Split(','), Ct);

        saved.IsFailure.ShouldBeTrue();
        saved.Messages.ShouldContain(m => m.Code == code);
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASaveWithNoListIsRefusedNamingActionsAndWritesNothing()
    {
        var fixture = new Fixture().SignedIn();

        var saved = await fixture.Service.Save("Alpha", null, Ct);

        saved.IsFailure.ShouldBeTrue();
        saved.Messages.ShouldContain(m => m.Code == "MESSAGING-21013");
        saved.CurrentMessage!.ShouldContain("actions");
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFailureToKeepAnActionTakesTheHalfSavedChoiceBackAndReportsIt()
    {
        var fixture = new Fixture().SignedIn();
        // Writes: delete actions (1), delete setting (2), insert setting (3), the first action (4) fails, then the choice is taken back (5, 6).
        fixture.FailWrite = 4;

        var saved = await fixture.Service.Save("Alpha", ["Dismiss", "Snooze"], Ct);

        saved.IsFailure.ShouldBeTrue();
        saved.Messages.ShouldContain(m => m.Code == "MESSAGING-71010");
        fixture.Writes.ShouldBe(["AlertKindSettingAction", "AlertKindSetting", "AlertKindSetting", "AlertKindSettingAction", "AlertKindSettingAction", "AlertKindSetting"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ResettingRemovesTheChoiceSoTheKindsOwnListAppliesAgain()
    {
        var fixture = new Fixture().SignedIn();
        fixture.CompanyChose("Alpha", "Dismiss");

        var reset = await fixture.Service.Reset("Alpha", Ct);

        reset.IsSuccess.ShouldBeTrue();
        reset.Value!.Configured.ShouldBeFalse();
        reset.Value.Actions.Select(a => a.Name).ShouldBe(["Acknowledge", "Snooze"]);
        fixture.Writes.ShouldBe(["AlertKindSettingAction", "AlertKindSetting"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ResettingAnUnknownKindIsRefused()
    {
        var fixture = new Fixture().SignedIn();

        var reset = await fixture.Service.Reset("Nothing", Ct);

        reset.IsFailure.ShouldBeTrue();
        reset.Messages.ShouldContain(m => m.Code == "MESSAGING-21010");
        fixture.Writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task NobodySignedInOrWithoutATenantCannotListSaveOrReset()
    {
        var anonymous = new Fixture();
        var noTenant = new Fixture();
        var context = new Mock<IAuthenticationContext>(MockBehavior.Loose);
        context.SetupGet(c => c.IsAuthenticated).Returns(true);
        context.SetupGet(c => c.UserId).Returns(Ana.ToString("D"));
        context.SetupGet(c => c.ActiveTenantId).Returns((Guid?)null);
        noTenant.Accessor.Current = context.Object;

        var listed = await anonymous.Service.List(Ct);
        var saved = await noTenant.Service.Save("Alpha", [], Ct);
        var reset = await noTenant.Service.Reset("Alpha", Ct);

        listed.Messages.ShouldContain(m => m.Code == "MESSAGING-51010");
        saved.Messages.ShouldContain(m => m.Code == "MESSAGING-51010");
        reset.Messages.ShouldContain(m => m.Code == "MESSAGING-51010");
        anonymous.Writes.ShouldBeEmpty();
        noTenant.Writes.ShouldBeEmpty();
    }
}
