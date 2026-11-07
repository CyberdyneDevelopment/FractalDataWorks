using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// FDW-3951: <see cref="ConnectionDisabledStates"/> reads a connection's state from the store for itself, disables it once with
/// the first reason standing, and enables it again with the reason kept, each change a new version of the one row.
/// </summary>
public sealed class ConnectionDisabledStatesTests
{
    private static readonly Guid ConnectionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TenantId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AConnectionWithNoRowIsNotDisabled()
    {
        var fixture = new Fixture();

        var result = await fixture.States.Read(ConnectionId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AConnectionWhoseCurrentRowHasNoEnabledAtIsDisabled()
    {
        var fixture = new Fixture(Row());

        var result = await fixture.States.Read(ConnectionId, TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull();
        result.Value.ConnectionId.ShouldBe(ConnectionId);
        result.Value.Reason.ShouldBe("three failed runs in a row");
        result.Value.DisabledBy.ShouldBe("threshold 'x' rule 1");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AConnectionEnabledAgainIsNotDisabled()
    {
        var enabled = Row();
        enabled.EnabledAt = Now;
        enabled.EnabledReason = "the query was fixed";
        var fixture = new Fixture(enabled);

        var result = await fixture.States.Read(ConnectionId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TheStateIsReadFromTheStoreAndNotFromTheCache()
    {
        var fixture = new Fixture();

        await fixture.States.Read(ConnectionId, TestContext.Current.CancellationToken);

        fixture.ReadsThroughTheCache.ShouldBe([false]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ARowThatCannotBeReadIsAFailureAndNotAnEnabledConnection()
    {
        var fixture = new Fixture { ReadFailure = GenericResult<IEnumerable<ConnectionDisabledConfiguration>>.Failure(new Fdw.Messages.GenericMessage("store down")) };

        var result = await fixture.States.Read(ConnectionId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task DisablingAConnectionSavesAFirstRowStampedWithItsTenant()
    {
        var fixture = new Fixture();

        var result = await fixture.States.Disable(ConnectionId, TenantId, false, "three failed runs in a row", "threshold 'x' rule 1", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var saved = fixture.Saves.ShouldHaveSingleItem();
        saved.Row.ConnectionId.ShouldBe(ConnectionId);
        saved.Row.TenantId.ShouldBe(TenantId);
        saved.Row.DisabledAt.ShouldBe(Now);
        saved.Row.DisabledReason.ShouldBe("three failed runs in a row");
        saved.Row.EnabledAt.ShouldBeNull();
        saved.ReplacesCurrent.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TheStateIsStampedWithTheTenantOfTheConnectionsOwnRowAndNotTheTenantOfWhoDisablesIt()
    {
        var owner = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var group = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var fixture = new Fixture { ConnectionTenant = owner, ConnectionGroup = group };

        var result = await fixture.States.Disable(ConnectionId, owner, false, "three failed runs in a row", "threshold 'x' rule 1", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var saved = fixture.Saves.ShouldHaveSingleItem().Row;
        saved.TenantId.ShouldBe(owner);
        saved.VisibilityGroupId.ShouldBe(group);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnotherTenantsRuleCannotDisableAConnectionAndNothingIsWritten()
    {
        var owner = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var fixture = new Fixture { ConnectionTenant = owner };

        var result = await fixture.States.Disable(ConnectionId, TenantId, false, "three failed runs in a row", "threshold 'x' rule 1", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        FailureCategory.IsForbidden(result).ShouldBeTrue();
        fixture.Saves.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARequesterTheCallerSaysMayDisableOtherTenantsConnectionsDisablesItStampedWithTheOwnersTenant()
    {
        var owner = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var fixture = new Fixture { ConnectionTenant = owner };

        var result = await fixture.States.Disable(ConnectionId, TenantId, true, "three failed runs in a row", "threshold 'x' rule 1", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        fixture.Saves.ShouldHaveSingleItem().Row.TenantId.ShouldBe(owner);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnotherTenantsRuleIsRefusedEvenWhenTheConnectionIsAlreadyDisabled()
    {
        var owner = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var fixture = new Fixture(Row()) { ConnectionTenant = owner };

        var result = await fixture.States.Disable(ConnectionId, TenantId, false, "a second reason", "threshold 'y' rule 2", TestContext.Current.CancellationToken);

        FailureCategory.IsForbidden(result).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AConnectionWithNoRowTheReaderCanSeeIsNotDisabledAndNoTenantIsAssumed()
    {
        var fixture = new Fixture { ConnectionTenant = null };

        var result = await fixture.States.Disable(ConnectionId, TenantId, false, "three failed runs in a row", "threshold 'x' rule 1", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        fixture.Saves.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AConnectionRowThatCannotBeReadIsNotDisabled()
    {
        var fixture = new Fixture { ConnectionReadFailure = GenericResult<IEnumerable<ConnectionTenantConfiguration>>.Failure(new Fdw.Messages.GenericMessage("store down")) };

        var result = await fixture.States.Disable(ConnectionId, TenantId, false, "three failed runs in a row", "threshold 'x' rule 1", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        fixture.Saves.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ADisabledConnectionIsLeftAloneAndTheFirstReasonStands()
    {
        var fixture = new Fixture(Row());

        var result = await fixture.States.Disable(ConnectionId, TenantId, false, "a second reason", "threshold 'y' rule 2", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Reason.ShouldBe("three failed runs in a row");
        fixture.Saves.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task DisablingAnEnabledAgainConnectionSavesANewVersionOfTheSameRow()
    {
        var enabled = Row();
        enabled.EnabledAt = Now.AddHours(-1);
        enabled.EnabledReason = "the query was fixed";
        var fixture = new Fixture(enabled);

        var result = await fixture.States.Disable(ConnectionId, TenantId, false, "failed again", "threshold 'x' rule 1", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var saved = fixture.Saves.ShouldHaveSingleItem();
        saved.Row.Id.ShouldBe(enabled.Id);
        saved.Row.EnabledAt.ShouldBeNull();
        saved.Row.EnabledReason.ShouldBeNull();
        saved.ReplacesCurrent.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AFailedSaveFailsTheDisable()
    {
        var fixture = new Fixture { SaveFailure = GenericResult<ConfigurationWriteResult>.Failure(new Fdw.Messages.GenericMessage("write refused")) };

        var result = await fixture.States.Disable(ConnectionId, TenantId, false, "three failed runs in a row", "threshold 'x' rule 1", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // A write the row-level security policy refuses (the MSSQL-100011 message of error 33504) stays a forbidden failure; another failed save does not (FDW-4193).
    private static IGenericResult<ConfigurationWriteResult> RefusedByPolicy() =>
        GenericResult<ConfigurationWriteResult>.Failure(new Fdw.Messages.GenericMessage(Fdw.Messages.MessageSeverity.Warning, "refused by the policy", "MSSQL-100011"));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ADisableRefusedByTheSecurityPolicyStaysAForbiddenFailure()
    {
        var fixture = new Fixture { SaveFailure = RefusedByPolicy() };

        var result = await fixture.States.Disable(ConnectionId, TenantId, false, "three failed runs in a row", "threshold 'x' rule 1", TestContext.Current.CancellationToken);

        FailureCategory.IsForbidden(result).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnEnableRefusedByTheSecurityPolicyStaysAForbiddenFailureAndAnotherFailedSaveDoesNot()
    {
        var refused = new Fixture(Row()) { SaveFailure = RefusedByPolicy() };
        var broken = new Fixture(Row()) { SaveFailure = GenericResult<ConfigurationWriteResult>.Failure(new Fdw.Messages.GenericMessage("write failed")) };

        var forbidden = await refused.States.Enable(ConnectionId, "a person looked", TestContext.Current.CancellationToken);
        var fault = await broken.States.Enable(ConnectionId, "a person looked", TestContext.Current.CancellationToken);

        FailureCategory.IsForbidden(forbidden).ShouldBeTrue();
        fault.IsFailure.ShouldBeTrue();
        FailureCategory.IsForbidden(fault).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheEnableRouteAnswers403ForARefusalAnd500ForAFaultWithOurSentenceAndNoDatabaseText()
    {
        var refused = new Fixture(Row()) { SaveFailure = RefusedByPolicy() };
        var broken = new Fixture(Row()) { SaveFailure = GenericResult<ConfigurationWriteResult>.Failure(new Fdw.Messages.GenericMessage("target object 'devConfigurationDb.conn.ConnectionDisabled' failed")) };

        var forbidden = await refused.States.Enable(ConnectionId, "a person looked", TestContext.Current.CancellationToken);
        var fault = await broken.States.Enable(ConnectionId, "a person looked", TestContext.Current.CancellationToken);

        Fdw.Services.Connections.Endpoints.EnableConnectionAnswers.StatusOf(forbidden).ShouldBe(403);
        Fdw.Services.Connections.Endpoints.EnableConnectionAnswers.StatusOf(fault).ShouldBe(500);
        Fdw.Services.Connections.Endpoints.EnableConnectionAnswers.SentenceOf(forbidden).ShouldContain("another tenant");
        Fdw.Services.Connections.Endpoints.EnableConnectionAnswers.SentenceOf(fault).ShouldNotContain("devConfigurationDb");
        Fdw.Services.Connections.Endpoints.EnableConnectionAnswers.SentenceOf(forbidden).ShouldNotContain("devConfigurationDb");
    }

    [Theory]
    [InlineData("", "threshold 'x' rule 1")]
    [InlineData("  ", "threshold 'x' rule 1")]
    [InlineData("three failed runs in a row", "")]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ADisableWithoutItsReasonOrItsAuthorIsRefusedBeforeTheStoreIsTouched(string reason, string disabledBy)
    {
        var fixture = new Fixture();

        var result = await fixture.States.Disable(ConnectionId, TenantId, false, reason, disabledBy, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        fixture.ReadsThroughTheCache.ShouldBeEmpty();
        fixture.Saves.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task EnablingADisabledConnectionSavesANewVersionCarryingTheReasonAndKeepingTheDisable()
    {
        var disabled = Row();
        var fixture = new Fixture(disabled);

        var result = await fixture.States.Enable(ConnectionId, "the query was fixed", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var saved = fixture.Saves.ShouldHaveSingleItem();
        saved.Row.Id.ShouldBe(disabled.Id);
        saved.Row.EnabledAt.ShouldBe(Now);
        saved.Row.EnabledReason.ShouldBe("the query was fixed");
        saved.Row.DisabledReason.ShouldBe("three failed runs in a row");
        saved.Row.TenantId.ShouldBe(disabled.TenantId);
        saved.ReplacesCurrent.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task EnablingAConnectionThatIsNotDisabledLeavesItAlone()
    {
        var fixture = new Fixture();

        var result = await fixture.States.Enable(ConnectionId, "the query was fixed", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        fixture.Saves.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task EnablingWithoutAReasonIsRefused()
    {
        var fixture = new Fixture(Row());

        var result = await fixture.States.Enable(ConnectionId, " ", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        fixture.Saves.ShouldBeEmpty();
    }

    private static ConnectionDisabledConfiguration Row() => new()
    {
        Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
        ConnectionId = ConnectionId,
        DisabledAt = Now.AddHours(-2),
        DisabledReason = "three failed runs in a row",
        DisabledBy = "threshold 'x' rule 1",
        TenantId = TenantId,
    };

    // The store holds at most one current row of the connection; saves are recorded with whether they replace it.
    private sealed class Fixture
    {
        private readonly ConnectionDisabledConfiguration? _current;

        public Fixture(ConnectionDisabledConfiguration? current = null)
        {
            _current = current;

            var gateway = new Mock<IConfigurationGateway>();
            gateway
                .Setup(g => g.Execute<IEnumerable<ConnectionDisabledConfiguration>>(
                    It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IDataCommand _, DataStoreTarget _, bool useCache, CancellationToken _) =>
                {
                    ReadsThroughTheCache.Add(useCache);
                    return ReadFailure ?? GenericResult<IEnumerable<ConnectionDisabledConfiguration>>.Success(
                        _current is null ? [] : [_current]);
                });
            gateway
                .Setup(g => g.Execute<IEnumerable<ConnectionTenantConfiguration>>(
                    It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IDataCommand _, DataStoreTarget _, bool _, CancellationToken _) =>
                    ConnectionReadFailure ?? GenericResult<IEnumerable<ConnectionTenantConfiguration>>.Success(
                        ConnectionTenant is { } tenant ? [new ConnectionTenantConfiguration { Id = ConnectionId, TenantId = tenant, VisibilityGroupId = ConnectionGroup }] : []));
            gateway
                .Setup(g => g.Execute<ConfigurationWriteResult>(
                    It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IDataCommand command, DataStoreTarget _, CancellationToken _) =>
                {
                    if (SaveFailure is { } failure)
                    {
                        return failure;
                    }

                    var save = (ConfigurationSaveCommand<ConnectionDisabledConfiguration>)command;
                    Saves.Add((save.Data, save.ReplacesCurrent));
                    return GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult([], new Dictionary<string, object?>()));
                });

            var gateways = new Mock<IConfigurationGatewayProvider>();
            gateways.Setup(p => p.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));

            States = new ConnectionDisabledStates(gateways.Object, "ConfigurationDb", new FixedTime(Now), NullLogger<ConnectionDisabledStates>.Instance);
        }

        public ConnectionDisabledStates States { get; }

        public List<bool> ReadsThroughTheCache { get; } = [];

        public List<(ConnectionDisabledConfiguration Row, bool ReplacesCurrent)> Saves { get; } = [];

        public IGenericResult<IEnumerable<ConnectionDisabledConfiguration>>? ReadFailure { get; set; }

        // The tenant of the connection's own row; none when the reader cannot see the row.
        public Guid? ConnectionTenant { get; set; } = TenantId;

        public Guid? ConnectionGroup { get; set; }

        public IGenericResult<IEnumerable<ConnectionTenantConfiguration>>? ConnectionReadFailure { get; set; }

        public IGenericResult<ConfigurationWriteResult>? SaveFailure { get; set; }
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
