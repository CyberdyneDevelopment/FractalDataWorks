using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Configuration;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Abstractions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-4459: a write of a data store asks the cross-tenant change policy about the tenant the store belongs to before it is made. A refusal names the setting and nothing is
/// written; an allowed write is made as the store's tenant and the caller's own context comes back; a store whose tenant cannot be read is never written.
/// </summary>
public sealed class DataStoreCrossTenantWriteTests
{
    private static readonly Guid StoreId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid OwnerTenant = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid OtherTenant = Guid.Parse("99999999-9999-9999-9999-999999999999");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnUpdateByAnotherTenantsAdministratorTheSettingForbidsIsRefusedNamingTheSettingAndNothingIsWritten()
    {
        var fixture = new Fixture(ActingAs(OtherTenant)) { Policy = { Refuses = true } };

        var result = await fixture.Provider.UpdateStore(Store(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        FailureCategory.IsForbidden(result).ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(CrossTenantChangeOption.DataStoreWrite);
        fixture.Policy.Asked.ShouldHaveSingleItem().ShouldBe((CrossTenantChangeOption.DataStoreWrite, OwnerTenant, (Guid?)OtherTenant));
        fixture.WriteAttempts.ShouldBe(0);
        fixture.Accessor.Current.ShouldBe(fixture.Accessor.Caller);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ADeleteByAnotherTenantsAdministratorTheSettingForbidsIsRefusedNamingTheSettingAndNothingIsWritten()
    {
        var fixture = new Fixture(ActingAs(OtherTenant)) { Policy = { Refuses = true } };

        var result = await fixture.Provider.DeleteStore(StoreId, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        FailureCategory.IsForbidden(result).ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(CrossTenantChangeOption.DataStoreWrite);
        fixture.WriteAttempts.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnUpdateByAnotherTenantsAdministratorTheSettingAllowsReachesTheWriteAndTheCallersContextComesBack()
    {
        var fixture = new Fixture(ActingAs(OtherTenant));

        var result = await fixture.Provider.UpdateStore(Store(), TestContext.Current.CancellationToken);

        // No implementation provider is registered for the store's kind, so the write itself fails; that it fails there and not as a refusal shows it was reached.
        result.IsFailure.ShouldBeTrue();
        FailureCategory.IsForbidden(result).ShouldBeFalse();
        fixture.Policy.Asked.ShouldHaveSingleItem().ShouldBe((CrossTenantChangeOption.DataStoreWrite, OwnerTenant, (Guid?)OtherTenant));
        fixture.Accessor.Current.ShouldBe(fixture.Accessor.Caller);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnUpdateInTheStoresOwnTenantAsksThePolicyAndReachesTheWrite()
    {
        var fixture = new Fixture(ActingAs(OwnerTenant));

        var result = await fixture.Provider.UpdateStore(Store(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        FailureCategory.IsForbidden(result).ShouldBeFalse();
        fixture.Policy.Asked.ShouldHaveSingleItem().ShouldBe((CrossTenantChangeOption.DataStoreWrite, OwnerTenant, (Guid?)OwnerTenant));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AStoreTheReaderCannotSeeIsNeverWrittenAndThePolicyIsNotAsked()
    {
        var fixture = new Fixture(ActingAs(OtherTenant)) { Owners = [] };

        var result = await fixture.Provider.UpdateStore(Store(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        fixture.Policy.Asked.ShouldBeEmpty();
        fixture.WriteAttempts.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AStoreWhoseTenantCannotBeReadIsNeverWrittenAndThePolicyIsNotAsked()
    {
        var fixture = new Fixture(ActingAs(OtherTenant)) { ReadFailure = GenericResult<IEnumerable<DataStoreTenantConfiguration>>.Failure(new Fdw.Messages.GenericMessage("the store is not reachable")) };

        var result = await fixture.Provider.DeleteStore(StoreId, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        fixture.Policy.Asked.ShouldBeEmpty();
        fixture.WriteAttempts.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task PermitWriteRefusesAnotherTenantsAdministratorTheSettingForbidsNamingTheSettingWithoutWritingAnything()
    {
        var fixture = new Fixture(ActingAs(OtherTenant)) { Policy = { Refuses = true } };

        var result = await fixture.Provider.PermitWrite(StoreId, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        FailureCategory.IsForbidden(result).ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(CrossTenantChangeOption.DataStoreWrite);
        fixture.Policy.Asked.ShouldHaveSingleItem().ShouldBe((CrossTenantChangeOption.DataStoreWrite, OwnerTenant, (Guid?)OtherTenant));
        fixture.WriteAttempts.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task PermitWriteAllowsWhatThePolicyAllowsAndFailsForAStoreTheReaderCannotSee()
    {
        var allowed = new Fixture(ActingAs(OtherTenant));
        var unseen = new Fixture(ActingAs(OtherTenant)) { Owners = [] };

        var permitted = await allowed.Provider.PermitWrite(StoreId, TestContext.Current.CancellationToken);
        var missing = await unseen.Provider.PermitWrite(StoreId, TestContext.Current.CancellationToken);

        permitted.IsSuccess.ShouldBeTrue();
        missing.IsFailure.ShouldBeTrue();
        unseen.Policy.Asked.ShouldBeEmpty();
    }

    private static IAuthenticationContext ActingAs(Guid tenant)
    {
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.UserId).Returns("11111111-1111-1111-1111-111111111111");
        caller.SetupGet(c => c.IsSystemContext).Returns(false);
        caller.SetupGet(c => c.ActiveTenantId).Returns(tenant);
        return caller.Object;
    }

    private static DataStoreImplementationConfiguration Store() => new()
    {
        Id = StoreId,
        Name = "NflDb",
        Domain = "DataStore",
        Implementation = "MsSql",
        Paths = [],
    };

    private sealed class Fixture
    {
        public Fixture(IAuthenticationContext caller)
        {
            Accessor = new TestAccessor { Caller = caller, Current = caller };

            var gateway = new Mock<IConfigurationGateway>();
            gateway
                .Setup(g => g.Execute<IEnumerable<DataStoreTenantConfiguration>>(
                    It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IDataCommand _, DataStoreTarget _, bool _, CancellationToken _) =>
                    ReadFailure ?? GenericResult<IEnumerable<DataStoreTenantConfiguration>>.Success(
                        Owners ?? [new DataStoreTenantConfiguration { Id = StoreId, TenantId = OwnerTenant }]));
            gateway
                .Setup(g => g.Execute<ConfigurationWriteResult>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IDataCommand _, DataStoreTarget _, CancellationToken _) =>
                {
                    WriteAttempts++;
                    return GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult([], new Dictionary<string, object?>()));
                });

            var gateways = new Mock<IConfigurationGatewayProvider>();
            gateways.Setup(p => p.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));

            Provider = new DataStoreConfigurationProvider(
                NullLogger<DataStoreConfigurationProvider>.Instance, gateways.Object, "ConfigurationDb", "data", "DataStore", new UnavailablePlacement(), Accessor, Policy);
        }

        public DataStoreConfigurationProvider Provider { get; }

        public TestAccessor Accessor { get; }

        public PolicyDouble Policy { get; } = new();

        public int WriteAttempts { get; private set; }

        public IReadOnlyList<DataStoreTenantConfiguration>? Owners { get; init; }

        public IGenericResult<IEnumerable<DataStoreTenantConfiguration>>? ReadFailure { get; init; }
    }

    private sealed class TestAccessor : IAuthenticationContextAccessor
    {
        public IAuthenticationContext? Caller { get; init; }

        public IAuthenticationContext? Current { get; set; }
    }

    // Records what the provider asks the policy and answers the refusal the real policy gives when the setting is off.
    private sealed class PolicyDouble : ICrossTenantChangePolicy
    {
        public bool Refuses { get; set; }

        public List<(string ChangeName, Guid OwnerTenantId, Guid? ActorTenantId)> Asked { get; } = [];

        public IGenericResult Permit(string changeName, Guid ownerTenantId, Guid? actorTenantId)
        {
            Asked.Add((changeName, ownerTenantId, actorTenantId));
            return Refuses
                ? GenericResult.Failure(CrossTenantChangeLog.Refused(NullLogger.Instance, changeName, ownerTenantId, actorTenantId ?? Guid.Empty))
                : GenericResult.Success();
        }
    }
}
