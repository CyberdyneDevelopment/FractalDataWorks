using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
// Why: Aliases disambiguate from Fdw.Services.Scheduling.Commands.Data
// and Fdw.Services.Scheduling.Data namespace segments.
using FilterGroup = Fdw.Data.FilterGroup;
using IFilterCondition = Fdw.Data.Abstractions.IFilterCondition;
using IFilterNode = Fdw.Data.Abstractions.IFilterNode;
using ScheduleQueryCommand = Fdw.Commands.Data.QueryCommand<Fdw.Services.Scheduling.Data.ScheduleQueryRecord>;

namespace Fdw.Services.Scheduling.Tests;

/// <summary>
/// Proves that DefaultSchedulingFactory delivers the per-request ITenantContext to
/// DefaultSchedulingService so that tenant filtering is actually applied when GetSchedules
/// is called. This is the regression test for FDW-526.
///
/// The captive-dependency bug: DefaultSchedulingFactory was registered TryAddSingleton and
/// injected ITenantContext at construction time (root scope). The root-scope context always
/// has HasTenant=false/TenantId=null, so tenant filtering was never applied regardless of
/// which authenticated tenant made the request.
///
/// The fix: factory injects IHttpContextAccessor (singleton) and resolves ITenantContext
/// from HttpContext.RequestServices at Create() time — delivering the request's scoped context.
/// </summary>
public sealed class TenantIsolationTests
{
    private static DefaultSchedulerConfiguration CreateConfig() => new()
    {
        Id = Guid.CreateVersion7(),
        Name = "TestScheduler",
        DataStoreName = "ConfigurationDb",
        PathName = "sched",
        ScheduleContainerName = "Schedule"
    };

    /// <summary>
    /// When the HttpContext carries TenantA's ITenantContext, the DataGateway must receive
    /// a command that includes a TenantId filter for TenantA (via BeginOrGroup / IsNull).
    /// This proves the per-request context — not the stale root-scope context — reaches the query.
    /// </summary>
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Create_WithTenantContextInHttpContext_UsesRequestTenantForFiltering()
    {
        // Arrange — tenant A context
        var tenantAId = Guid.NewGuid();

        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(t => t.HasTenant).Returns(true);
        tenantContextMock.Setup(t => t.TenantId).Returns(tenantAId);

        // Build a per-request DI scope that has TenantA's context registered
        var services = new ServiceCollection();
        services.AddSingleton(tenantContextMock.Object);
        var requestSp = services.BuildServiceProvider();

        // HttpContext carries the per-request scope
        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(h => h.RequestServices).Returns(requestSp);

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContextMock.Object);

        // Capture the IDataCommand passed to IDataGateway.Execute — this is where tenant filtering lives
        IDataCommand? capturedCommand = null;
        var dataGatewayMock = new Mock<IDataGateway>();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<Data.ScheduleQueryRecord>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((cmd, _, _) => capturedCommand = cmd)
            .ReturnsAsync(GenericResult<IEnumerable<Data.ScheduleQueryRecord>>.Success(
                Array.Empty<Data.ScheduleQueryRecord>()));

        var factory = new DefaultSchedulingFactory(
            NullLoggerFactory.Instance,
            dataGatewayMock.Object,
            httpContextAccessorMock.Object);

        // Act — Create() resolves ITenantContext from the mock HttpContext.RequestServices
        var createResult = factory.Create(CreateConfig());
        createResult.IsSuccess.ShouldBeTrue("factory.Create must succeed with a valid configuration");
        var service = createResult.Value!;

        // Trigger GetSchedules so the DataGateway is called and we can inspect the command
        var schedulesResult = await service.GetSchedules(cancellationToken: TestContext.Current.CancellationToken);

        // Assert — DataGateway must have been called (meaning the service was constructed correctly)
        dataGatewayMock.Verify(
            g => g.Execute<IEnumerable<Data.ScheduleQueryRecord>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "DataGateway.Execute must be called by GetSchedules");

        capturedCommand.ShouldNotBeNull("a command must have been sent to the DataGateway");

        // Why: The filter expression is the authoritative evidence that the correct tenant reached
        // the query builder. We inspect the command's FilterExpression to confirm that a TenantId
        // filter targeting TenantA's GUID is present.
        var commandText = capturedCommand!.ToString() ?? string.Empty;
        // The command captures filters — confirm TenantA's ID appears somewhere in the command
        // representation (either ToString or via reflection on FilterExpression).
        // The OR-group filter: TenantId = tenantAId OR TenantId IS NULL must be present.
        AssertTenantFilterPresent(capturedCommand, tenantAId);
    }

    /// <summary>
    /// Two requests with different tenant contexts must produce commands with different tenant filters.
    /// This is the core regression test: if the root-scope context was captured (the bug), both
    /// requests would have HasTenant=false and no filter would be applied — making them identical
    /// and cross-contaminating tenant data.
    /// </summary>
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Create_TwoRequestsWithDifferentTenants_ProduceDifferentTenantFilters()
    {
        // Arrange — tenant A
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();

        IDataCommand? commandForTenantA = null;
        IDataCommand? commandForTenantB = null;

        // Factory A — simulates request from tenant A
        var serviceA = CreateServiceWithTenant(tenantAId,
            captureCallback: cmd => commandForTenantA = cmd);

        // Factory B — simulates request from tenant B (different HttpContext/scope)
        var serviceB = CreateServiceWithTenant(tenantBId,
            captureCallback: cmd => commandForTenantB = cmd);

        // Act
        await serviceA.GetSchedules(cancellationToken: TestContext.Current.CancellationToken);
        await serviceB.GetSchedules(cancellationToken: TestContext.Current.CancellationToken);

        // Assert — both commands must carry tenant filters, and those filters must differ
        commandForTenantA.ShouldNotBeNull();
        commandForTenantB.ShouldNotBeNull();

        AssertTenantFilterPresent(commandForTenantA!, tenantAId);
        AssertTenantFilterPresent(commandForTenantB!, tenantBId);

        // The two commands must NOT contain each other's tenant ID —
        // proving the filters are truly per-tenant, not shared.
        AssertTenantFilterAbsent(commandForTenantA!, tenantBId);
        AssertTenantFilterAbsent(commandForTenantB!, tenantAId);
    }

    /// <summary>
    /// When there is no HttpContext (background worker, test scenario), the factory must still
    /// produce a service — tenant filtering is simply skipped (returns all schedules).
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Scheduling")]
    public async Task Create_WithNoHttpContext_ProducesServiceWithNoTenantFilter()
    {
        // Arrange — accessor returns null HttpContext (background task scenario)
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext)null!);

        IDataCommand? capturedCommand = null;
        var dataGatewayMock = new Mock<IDataGateway>();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<Data.ScheduleQueryRecord>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((cmd, _, _) => capturedCommand = cmd)
            .ReturnsAsync(GenericResult<IEnumerable<Data.ScheduleQueryRecord>>.Success(
                Array.Empty<Data.ScheduleQueryRecord>()));

        var factory = new DefaultSchedulingFactory(
            NullLoggerFactory.Instance,
            dataGatewayMock.Object,
            httpContextAccessorMock.Object);

        var createResult = factory.Create(CreateConfig());
        createResult.IsSuccess.ShouldBeTrue();

        // Act
        var result = await createResult.Value!.GetSchedules(cancellationToken: TestContext.Current.CancellationToken);

        // Assert — service must not crash and command must have been issued (no tenant filter)
        dataGatewayMock.Verify(
            g => g.Execute<IEnumerable<Data.ScheduleQueryRecord>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()),
            Times.Once);

        capturedCommand.ShouldNotBeNull();
        // Without a tenant context the command has no tenant filter — any tenant's data is returned.
        // This is correct for background workers (e.g., cron trigger daemon) that operate globally.
        AssertNoTenantFilterPresent(capturedCommand!);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IFrameworkSchedulingService CreateServiceWithTenant(
        Guid tenantId,
        Action<IDataCommand> captureCallback)
    {
        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(t => t.HasTenant).Returns(true);
        tenantContextMock.Setup(t => t.TenantId).Returns(tenantId);

        var requestServices = new ServiceCollection();
        requestServices.AddSingleton(tenantContextMock.Object);
        var requestSp = requestServices.BuildServiceProvider();

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(h => h.RequestServices).Returns(requestSp);

        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(a => a.HttpContext).Returns(httpContextMock.Object);

        var dataGatewayMock = new Mock<IDataGateway>();
        dataGatewayMock
            .Setup(g => g.Execute<IEnumerable<Data.ScheduleQueryRecord>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((cmd, _, _) => captureCallback(cmd))
            .ReturnsAsync(GenericResult<IEnumerable<Data.ScheduleQueryRecord>>.Success(
                Array.Empty<Data.ScheduleQueryRecord>()));

        var factory = new DefaultSchedulingFactory(
            NullLoggerFactory.Instance,
            dataGatewayMock.Object,
            accessorMock.Object);

        return factory.Create(CreateConfig()).Value!;
    }

    /// <summary>
    /// Inspects a <see cref="Commands.Data.QueryCommand{T}"/> for the presence of a tenant-ID
    /// filter targeting <paramref name="tenantId"/>.
    /// </summary>
    /// <remarks>
    /// DefaultSchedulingService builds a BeginOrGroup filter containing two conditions:
    ///   TenantId = tenantId  (Equal operator)
    ///   TenantId IS NULL     (IsNull operator)
    /// We walk the IFilterExpression root tree using the public IFilterCondition and FilterGroup
    /// interfaces and verify at least one Equal condition targeting the expected GUID is present.
    /// </remarks>
    private static void AssertTenantFilterPresent(IDataCommand command, Guid tenantId)
    {
        // Why: QueryCommand<T>.Filter is the typed IFilterExpression — cast via the public interface
        // defined on the concrete command type. IDataCommand itself does not expose Filter, so we
        // cast to the known type. This is test-internal — it does not escape the test boundary.
        var queryCommand = command as ScheduleQueryCommand;
        queryCommand.ShouldNotBeNull(
            $"Expected QueryCommand<ScheduleQueryRecord> but got {command.GetType().Name}");

        var filter = queryCommand!.Filter;
        filter.ShouldNotBeNull(
            $"QueryCommand.Filter must not be null — tenant filter for {tenantId} must be present");

        ContainsGuidConditionValue(filter!.Root, tenantId)
            .ShouldBeTrue(
                $"DataCommand filter must contain a TenantId = {tenantId} condition. " +
                $"This proves the per-request ITenantContext reached the query builder.");
    }

    private static void AssertTenantFilterAbsent(IDataCommand command, Guid tenantId)
    {
        var queryCommand = command as ScheduleQueryCommand;
        if (queryCommand?.Filter is null)
            return; // No filter at all — cannot contain the absent tenant

        ContainsGuidConditionValue(queryCommand.Filter.Root, tenantId)
            .ShouldBeFalse(
                $"DataCommand must NOT contain a TenantId = {tenantId} condition — " +
                $"cross-tenant data leak detected.");
    }

    private static void AssertNoTenantFilterPresent(IDataCommand command)
    {
        // Why: Without a tenant context (background worker, no HttpContext), the service
        // must not apply any GUID-valued TenantId filter. The filter may still exist for
        // other conditions (e.g., IsEnabled) but must not contain any GUID values.
        var queryCommand = command as ScheduleQueryCommand;
        if (queryCommand?.Filter is null)
            return; // No filter — correct for no-tenant case

        ContainsAnyGuidConditionValue(queryCommand.Filter.Root)
            .ShouldBeFalse(
                "Without a tenant context, the command must not contain any GUID-valued filter condition. " +
                "Background workers see all schedules (no tenant filtering).");
    }

    /// <summary>
    /// Recursively walks the filter tree and returns true if any leaf condition
    /// has a <see cref="Guid"/> value equal to <paramref name="targetId"/>.
    /// </summary>
    private static bool ContainsGuidConditionValue(IFilterNode? node, Guid targetId)
    {
        if (node is null) return false;

        if (node is IFilterCondition condition)
        {
            return condition.Value is Guid g && g == targetId;
        }

        if (node is FilterGroup group)
        {
            foreach (var child in group.Nodes)
                if (ContainsGuidConditionValue(child, targetId)) return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if any leaf condition in the filter tree has a <see cref="Guid"/> value.
    /// </summary>
    private static bool ContainsAnyGuidConditionValue(IFilterNode? node)
    {
        if (node is null) return false;

        if (node is IFilterCondition condition)
            return condition.Value is Guid;

        if (node is FilterGroup group)
        {
            foreach (var child in group.Nodes)
                if (ContainsAnyGuidConditionValue(child)) return true;
        }

        return false;
    }
}
