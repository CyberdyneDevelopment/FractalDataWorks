using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Operations.Data;
using Fdw.Operations.Execution;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Tests.Execution;

/// <summary>
/// Every execution item belongs to one tenant, and every read and control reaches only the items of the
/// tenant the caller acts in (FDW-1073).
/// </summary>
public sealed class ExecutionTrackingServiceTenantTests
{
    private static readonly Guid TenantA = Guid.Parse("0a000000-0000-0000-0000-00000000000a");
    private static readonly Guid TenantB = Guid.Parse("0b000000-0000-0000-0000-00000000000b");

    private sealed class StubGatewayProvider(IDataGateway gateway) : IDataGatewayProvider
    {
        public IDataGateway ByName(string name) => gateway;
    }

    private readonly Mock<IDataGateway> _gateway = new();
    private readonly List<IDataCommand> _writes = [];
    private readonly List<IDataCommand> _itemQueries = [];

    private void Returns(params ExecutionItem[] items)
    {
        _gateway
            .Setup(g => g.Execute<IEnumerable<ExecutionItem>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((c, _, _) => _itemQueries.Add(c))
            .ReturnsAsync(GenericResult<IEnumerable<ExecutionItem>>.Success(items));
        _gateway
            .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((c, _, _) => _writes.Add(c))
            .ReturnsAsync(GenericResult<int>.Success(1));
        _gateway
            .Setup(g => g.Execute<IEnumerable<ExecutionEvent>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ExecutionEvent>>.Success(Array.Empty<ExecutionEvent>()));
    }

    private ExecutionTrackingService Tracker(IAuthenticationContext? caller)
    {
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(caller);
        return new ExecutionTrackingService(
            new StubGatewayProvider(_gateway.Object), NullLoggerFactory.Instance, accessor.Object, "OpsDb");
    }

    private static IAuthenticationContext ActingIn(Guid? tenantId, bool global = false)
    {
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.ActiveTenantId).Returns(tenantId);
        caller.SetupGet(c => c.IsSystemContext).Returns(false);
        caller.SetupGet(c => c.Claims).Returns(global
            ? new Dictionary<string, object>(StringComparer.Ordinal) { ["globalTenant"] = "true" }
            : new Dictionary<string, object>(StringComparer.Ordinal));
        return caller.Object;
    }

    private static ExecutionItem Item(Guid tenantId, string state = "Running", string type = "Job")
    {
        var id = Guid.NewGuid();
        return new ExecutionItem
        {
            Id = id,
            RootExecutionItemId = id,
            ItemType = type,
            Name = "Run",
            State = state,
            TenantId = tenantId
        };
    }

    private ExecutionItem? InsertedItem()
        => _writes.OfType<InsertCommand<ExecutionItem>>().SelectMany(c => c.Data).SingleOrDefault();

    private static IEnumerable<FilterCondition> Conditions(IFilterNode? node) => node switch
    {
        FilterCondition c => [c],
        FilterGroup g => g.Nodes.SelectMany(Conditions),
        _ => []
    };

    [Fact]
    public async Task RootItemWithoutTenantIsRefusedAndNotWritten()
    {
        Returns();

        var result = await Tracker(ActingIn(null)).CreateItem(
            ExecutionItemTypes.Job, "Run", tenantId: null, cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        InsertedItem().ShouldBeNull();
    }

    [Fact]
    public async Task RootItemIsWrittenWithTheTenantItStates()
    {
        Returns();

        var result = await Tracker(ActingIn(TenantA)).CreateItem(
            ExecutionItemTypes.Job, "Run", TenantA, cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        InsertedItem()!.TenantId.ShouldBe(TenantA);
    }

    [Fact]
    public async Task CallerActingInOneTenantCannotCreateForAnother()
    {
        Returns();

        var result = await Tracker(ActingIn(TenantA)).CreateItem(
            ExecutionItemTypes.Job, "Run", TenantB, cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        InsertedItem().ShouldBeNull();
    }

    [Fact]
    public async Task ChildTakesItsParentsTenant()
    {
        var parent = Item(TenantB);
        Returns(parent);

        var result = await Tracker(new SystemAuthenticationContext()).CreateItem(
            ExecutionItemTypes.ByName("Task"), "Step", tenantId: null, parentId: parent.Id,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        InsertedItem()!.TenantId.ShouldBe(TenantB);
    }

    [Fact]
    public async Task ChildStatingAnotherTenantThanItsParentIsRefused()
    {
        var parent = Item(TenantB);
        Returns(parent);

        var result = await Tracker(new SystemAuthenticationContext()).CreateItem(
            ExecutionItemTypes.ByName("Task"), "Step", TenantA, parentId: parent.Id,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        InsertedItem().ShouldBeNull();
    }

    [Fact]
    public async Task AnotherTenantsItemReadsAsNotFound()
    {
        var item = Item(TenantB);
        Returns(item);

        var result = await Tracker(ActingIn(TenantA)).GetItem(item.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task CallerActingInGlobalReadsEveryTenantsItem()
    {
        var item = Item(TenantB);
        Returns(item);

        var result = await Tracker(ActingIn(Guid.NewGuid(), global: true)).GetItem(item.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task AnotherTenantsRunCannotBeCancelled()
    {
        var item = Item(TenantB);
        Returns(item);

        var result = await Tracker(ActingIn(TenantA)).TransitionState(
            item.Id, ExecutionStateTypes.ByName("Cancelled"), cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _writes.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListCarriesTheCallersTenant()
    {
        Returns();

        await Tracker(ActingIn(TenantA)).ListExecutions(cancellationToken: TestContext.Current.CancellationToken);

        var filter = ((QueryCommand<ExecutionItem>)_itemQueries.Single()).Filter as FilterExpression;
        Conditions(filter?.Root).ShouldContain(c => c.PropertyName == "TenantId" && Equals(c.Value, TenantA));
    }

    [Fact]
    public async Task ListNarrowedToARootRunFiltersOnTheRootExecutionItem()
    {
        Returns();
        var run = Guid.NewGuid();

        await Tracker(ActingIn(TenantA)).ListExecutions(rootId: run, cancellationToken: TestContext.Current.CancellationToken);

        var filter = ((QueryCommand<ExecutionItem>)_itemQueries.Single()).Filter as FilterExpression;
        Conditions(filter?.Root).ShouldContain(c => c.PropertyName == "RootExecutionItemId" && Equals(c.Value, run));
        Conditions(filter?.Root).ShouldContain(c => c.PropertyName == "TenantId" && Equals(c.Value, TenantA));
    }

    [Fact]
    public async Task ListWithNoRootRunHasNoRootTerm()
    {
        Returns();

        await Tracker(ActingIn(TenantA)).ListExecutions(cancellationToken: TestContext.Current.CancellationToken);

        var filter = ((QueryCommand<ExecutionItem>)_itemQueries.Single()).Filter as FilterExpression;
        Conditions(filter?.Root).ShouldNotContain(c => c.PropertyName == "RootExecutionItemId");
    }

    [Fact]
    public async Task CallerWithNoTenantListsNothingAndQueriesNothing()
    {
        Returns(Item(TenantA));

        var result = await Tracker(ActingIn(null)).ListExecutions(cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Items.ShouldBeEmpty();
        _itemQueries.ShouldBeEmpty();
    }

    [Fact]
    public async Task SystemContextListsWithoutATenantTerm()
    {
        Returns();

        await Tracker(new SystemAuthenticationContext()).ListExecutions(cancellationToken: TestContext.Current.CancellationToken);

        var filter = ((QueryCommand<ExecutionItem>)_itemQueries.Single()).Filter as FilterExpression;
        Conditions(filter?.Root).ShouldNotContain(c => c.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EventsOfAnotherTenantsItemAreEmpty()
    {
        var item = Item(TenantB);
        Returns(item);

        var result = await Tracker(ActingIn(TenantA)).GetEvents(item.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldBeEmpty();
    }
}
