using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// A run acts with the roles it was started with (FDW-1161). A caller may name only roles they hold, in the tenant they act in,
/// and the roles must reach everything the pipeline reads and writes: every data set, the connection and data store behind each
/// of its sources, every connection it names, and every data store container it touches. A scheduled firing is judged for the
/// same reach without the firing caller holding the roles.
/// </summary>
public sealed class PipelineRunAuthorityTests
{
    private static readonly Guid TenantId = new("11111111-1111-1111-1111-111111111111");
    private static readonly StepReach NoSteps = new([], []);
    private static readonly PipelineReach Reach = new("Nightly", "Orders", "OrdersCopy", null, null, null, null, null, null, NoSteps);
    private static readonly PipelineReach StoreReach = new("Nightly", null, "OrdersCopy", "Feeds", "orders/v1", "orders", null, null, null, NoSteps);
    private static readonly PipelineReach DestinationStoreReach = new("Nightly", "Orders", null, null, null, null, "Warehouse", "orders/v1", "orders", NoSteps);

    private readonly Mock<IRoleGrants> _grants = new();
    private readonly Mock<IDataSetConfigurationProvider> _dataSets = new();
    private readonly Mock<IConnectionConfigurationProvider> _connections = new();
    private readonly Mock<IDataStoreProvider> _dataStores = new();

    private PipelineRunAuthority Build() => new(_grants.Object, _dataSets.Object, _connections.Object, _dataStores.Object);

    private static IAuthenticationContext Caller(Guid? tenantId, params string[] roles)
    {
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.ActiveTenantId).Returns(tenantId);
        caller.SetupGet(c => c.Roles).Returns(roles);
        caller.SetupGet(c => c.UserId).Returns("22222222-2222-2222-2222-222222222222");
        return caller.Object;
    }

    private void RolesCarry(params string[] permissions)
        => _grants
            .Setup(g => g.Get(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(permissions));

    private static IDataSetImplementationConfiguration DataSetOn(string connection, string? store)
    {
        var dataSet = new Mock<IDataSetImplementationConfiguration>();
        dataSet.SetupGet(d => d.Sources).Returns([new DataSetSourceConfiguration { ConnectionName = connection, DataStoreName = store ?? string.Empty }]);
        return dataSet.Object;
    }

    // Each named data set is visible and sits on the connection "Main" and the data store "Store"; every other name is not visible.
    private void Visible(params string[] dataSets)
    {
        _dataSets
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Failure(Mock.Of<IGenericMessage>()));
        foreach (var name in dataSets)
        {
            _dataSets
                .Setup(p => p.Get(name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(DataSetOn("Main", "Store")));
        }
    }

    private void ConnectionsVisible(params string[] names)
    {
        _connections
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IConnectionImplementationConfiguration>.Failure(Mock.Of<IGenericMessage>()));
        foreach (var name in names)
        {
            _connections
                .Setup(p => p.Get(name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IConnectionImplementationConfiguration>.Success(Mock.Of<IConnectionImplementationConfiguration>()));
        }
    }

    private void StoreVisible(string name, bool declaredByHost = false)
    {
        var store = new Mock<IDataStore>();
        store.SetupGet(s => s.DeclaredByHost).Returns(declaredByHost);
        _dataStores
            .Setup(p => p.Get(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStore>.Success(store.Object));
    }

    private void ContainerVisible(string store, string path, string container, bool visible)
        => _dataStores
            .Setup(p => p.Get(store, path, container, It.IsAny<CancellationToken>()))
            .ReturnsAsync(visible
                ? GenericResult<IDataContainer>.Success(Mock.Of<IDataContainer>())
                : GenericResult<IDataContainer>.Failure(Mock.Of<IGenericMessage>()));

    // Everything the plain reach touches is visible: both data sets, the connection and the store behind them.
    private void EverythingVisible()
    {
        Visible("Orders", "OrdersCopy");
        ConnectionsVisible("Main");
        StoreVisible("Store");
    }

    private void RolesCarryAll() => RolesCarry(
        "datasets:read", "datasets:write", "connections:read", "connections:write", "datastores:read", "datastores:write");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARunWithNoRoleIsRefused()
    {
        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), Reach, [],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _grants.Verify(g => g.Get(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task NoRolesAtAllIsRefusedRatherThanReadAsAnEmptyList()
    {
        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), Reach, null,
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _grants.Verify(g => g.Get(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerActingInNoTenantIsRefused()
    {
        var result = await Build().Authorize(Caller(null, "DataEngineer"), Reach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerCannotStartARunWithARoleTheyDoNotHold()
    {
        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), Reach, ["Admin"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _grants.Verify(g => g.Get(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task HeldRolesThatReachEverythingTheCallerCanSeeAreAccepted()
    {
        RolesCarryAll();
        EverythingVisible();

        var result = await Build().Authorize(Caller(TenantId, "dataengineer"), Reach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task RolesThatCannotWriteTheDestinationAreRefused()
    {
        RolesCarry("datasets:read", "connections:read", "connections:write", "datastores:read", "datastores:write");
        EverythingVisible();

        var result = await Build().Authorize(Caller(TenantId, "DataReader"), Reach, ["DataReader"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("datasets:write");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ASourceTheCallerCannotSeeIsRefusedEvenWhenTheRolesCouldReadIt()
    {
        RolesCarryAll();
        Visible("OrdersCopy");
        ConnectionsVisible("Main");
        StoreVisible("Store");

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), Reach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // FDW-1161: reading or writing a data set is reading or writing the connection and the data store its sources sit on.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task RolesThatCannotWriteTheConnectionBehindTheDestinationAreRefused()
    {
        RolesCarry("datasets:read", "datasets:write", "connections:read", "datastores:read", "datastores:write");
        EverythingVisible();

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), Reach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("connections:write");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task RolesThatCannotWriteTheDataStoreBehindTheDestinationAreRefused()
    {
        RolesCarry("datasets:read", "datasets:write", "connections:read", "connections:write", "datastores:read");
        EverythingVisible();

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), Reach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("datastores:write");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ADataStoreBehindADataSetTheCallerCannotSeeIsRefused()
    {
        RolesCarryAll();
        Visible("Orders", "OrdersCopy");
        ConnectionsVisible("Main");
        _dataStores
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStore>.Failure(Mock.Of<IGenericMessage>()));

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), Reach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // The data sets the stages and load steps touch are judged like the pipeline's own: a Union or a Lookup reads, a load or a
    // reject step writes.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ADataSetAStageReadsIsRefusedWhenTheCallerCannotSeeIt()
    {
        RolesCarryAll();
        EverythingVisible();
        var reach = Reach with { Steps = new StepReach([new ReachedDataSet("OrdersArchive", false)], []) };

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), reach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ADataSetAStageReadsIsAcceptedWhenTheCallerCanSeeItAndTheRolesCanReadIt()
    {
        RolesCarryAll();
        EverythingVisible();
        Visible("Orders", "OrdersCopy", "OrdersArchive");
        var reach = Reach with { Steps = new StepReach([new ReachedDataSet("OrdersArchive", false)], []) };

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), reach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ADataSetALoadStepWritesNeedsDatasetsWriteEvenWhenThePipelineHasNoDestinationOfItsOwn()
    {
        RolesCarry("datasets:read", "connections:read", "connections:write", "datastores:read", "datastores:write");
        Visible("Orders", "Rejects");
        ConnectionsVisible("Main");
        StoreVisible("Store");
        var reach = new PipelineReach("Nightly", "Orders", null, null, null, null, null, null, null,
            new StepReach([new ReachedDataSet("Rejects", true)], []));

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), reach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("datasets:write");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AContainerALookupReadsNeedsDatastoresRead()
    {
        RolesCarry("datasets:read", "datasets:write", "connections:read", "connections:write", "datastores:write");
        StoreVisible("Reference");
        ContainerVisible("Reference", "ref/v1", "customers", visible: true);
        var reach = new PipelineReach("Nightly", null, null, null, null, null, null, null, null,
            new StepReach([], [new ReachedContainer("Reference", "ref/v1", "customers", false)]));

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), reach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("datastores:read");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AContainerAnExecuteStepWritesNeedsDatastoresWrite()
    {
        RolesCarry("datasets:read", "datasets:write", "connections:read", "connections:write", "datastores:read");
        StoreVisible("Feeds");
        ContainerVisible("Feeds", "orders/v1", "orders", visible: true);
        var reach = new PipelineReach("Nightly", null, null, null, null, null, null, null, null,
            new StepReach([], [new ReachedContainer("Feeds", "orders/v1", "orders", true)]));

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), reach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("datastores:write");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AContainerAnExecuteStepWritesIsAcceptedWhenVisibleAndTheRolesCanWriteTheStore()
    {
        RolesCarryAll();
        EverythingVisible();
        StoreVisible("Feeds");
        ContainerVisible("Feeds", "orders/v1", "orders", visible: true);
        var reach = Reach with { Steps = new StepReach([], [new ReachedContainer("Feeds", "orders/v1", "orders", true)]) };

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), reach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AGrantThatSucceedsWithNothingIsAFailureNotAnAcceptance()
    {
        _grants
            .Setup(g => g.Get(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(null!));
        EverythingVisible();

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), Reach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AFailedGrantIsAFailure()
    {
        _grants
            .Setup(g => g.Get(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Failure(Mock.Of<IGenericMessage>()));

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), Reach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ADataStoreContainerSourceTheCallerCanSeeAndTheRolesCanReadIsAccepted()
    {
        RolesCarry("datastores:read", "datasets:write", "connections:write", "datastores:write");
        EverythingVisible();
        StoreVisible("Feeds");
        ContainerVisible("Feeds", "orders/v1", "orders", visible: true);

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), StoreReach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AStoreTheHostDeclaresIsNeverAPipelineSource()
    {
        RolesCarryAll();
        EverythingVisible();
        StoreVisible("Feeds", declaredByHost: true);
        ContainerVisible("Feeds", "orders/v1", "orders", visible: true);

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), StoreReach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AContainerTheCallerCannotSeeIsRefused()
    {
        RolesCarryAll();
        EverythingVisible();
        StoreVisible("Feeds");
        ContainerVisible("Feeds", "orders/v1", "orders", visible: false);

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), StoreReach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Validation")]
    public async Task AHalfAddressedDataStoreSourceIsRefused()
    {
        RolesCarryAll();
        EverythingVisible();
        var partial = StoreReach with { SourceContainerName = null };

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), partial, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Validation")]
    public async Task ADataStoreSourceBesideASourceDataSetIsRefused()
    {
        RolesCarryAll();
        EverythingVisible();
        StoreVisible("Feeds");
        ContainerVisible("Feeds", "orders/v1", "orders", visible: true);
        var doubled = StoreReach with { SourceDataSet = "Orders" };

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), doubled, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ADataStoreContainerDestinationTheCallerCanSeeAndTheRolesCanWriteIsAccepted()
    {
        RolesCarryAll();
        Visible("Orders");
        StoreVisible("Warehouse");
        ContainerVisible("Warehouse", "orders/v1", "orders", visible: true);

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), DestinationStoreReach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ADataStoreContainerDestinationNeedsDatastoresWrite()
    {
        RolesCarry("datasets:read", "datasets:write", "connections:read", "connections:write", "datastores:read");
        Visible("Orders");
        StoreVisible("Warehouse");
        ContainerVisible("Warehouse", "orders/v1", "orders", visible: true);

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), DestinationStoreReach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("datastores:write");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AStoreTheHostDeclaresIsNeverAPipelineDestination()
    {
        RolesCarryAll();
        Visible("Orders");
        StoreVisible("Warehouse", declaredByHost: true);
        ContainerVisible("Warehouse", "orders/v1", "orders", visible: true);

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), DestinationStoreReach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Validation")]
    public async Task AHalfAddressedDataStoreDestinationIsRefused()
    {
        RolesCarryAll();
        Visible("Orders");
        var partial = DestinationStoreReach with { DestinationContainerName = null };

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), partial, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Validation")]
    public async Task ADataStoreDestinationBesideADestinationDataSetIsRefused()
    {
        RolesCarryAll();
        Visible("Orders", "OrdersCopy");
        StoreVisible("Warehouse");
        ContainerVisible("Warehouse", "orders/v1", "orders", visible: true);
        var doubled = DestinationStoreReach with { DestinationDataSet = "OrdersCopy" };

        var result = await Build().Authorize(Caller(TenantId, "DataEngineer"), doubled, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // A schedule fires with no person behind it: its creator held the roles when it was saved, so the firing caller need not,
    // but the pipeline as it stands now must still be reachable with them.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AScheduledRunIsAcceptedWithoutTheFiringCallerHoldingTheRoles()
    {
        RolesCarryAll();
        EverythingVisible();

        var result = await Build().AuthorizeScheduled(Reach, ["DataEngineer"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AScheduledRunWithNoRolesIsRefused()
    {
        var result = await Build().AuthorizeScheduled(Reach, null, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _grants.Verify(g => g.Get(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AScheduledRunIsRefusedWhenThePipelineNowReachesWhatTheRolesCannotWrite()
    {
        RolesCarry("datasets:read", "connections:read", "datastores:read");
        EverythingVisible();

        var result = await Build().AuthorizeScheduled(Reach, ["DataReader"],
            NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
