using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// FDW-3097: the queue route lists the scheduler's own runs. A platform administrator reads every tenant's; anyone else reads their own
/// tenant's only, is not told which run of another tenant holds theirs back, and is refused when the request carries no tenant.
/// </summary>
[Trait("Category", "Security")]
public class PipelineRunQueueEndpointTests
{
    private static readonly Guid TenantA = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid TenantB = new("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly PipelineRunLimitValues Limits = new(8, 8);

    private sealed class Queue(PipelineRunScheduler scheduler, ITenantContext tenant) : ListPipelineRunQueueEndpointBase(NullLogger.Instance, scheduler, tenant);

    private static PipelineRunIdentity Run(string pipeline, Guid? tenant) => new(Guid.NewGuid(), Guid.NewGuid(), pipeline, tenant);

    private static PipelineRunClaim Claim(PipelineRunIdentity run, params string[] tables) => PipelineRunClaim.Writing(run, tables, Limits, null);

    private static Action<DefaultHttpContext> Caller(bool platformAdmin) => ctx =>
    {
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "caller")], "test"));
        var roles = new Mock<ISystemRoleConfiguration>();
        roles.Setup(r => r.IsPlatformAdmin(It.IsAny<ClaimsPrincipal>())).Returns(platformAdmin);
        roles.SetupGet(r => r.AdminRoleName).Returns("Admin");
        ctx.AddTestServices(s => s.AddSingleton(roles.Object));
    };

    private static ITenantContext Tenant(Guid? id)
    {
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(t => t.TenantId).Returns(id);
        return tenant.Object;
    }

    // Tenant A runs "Orders" writing store//t; tenant B's "Refunds" waits for that table; tenant B also has a run of its own in flight that holds
    // a second run of its own back.
    private static async Task<(PipelineRunScheduler Scheduler, PipelineRunIdentity A, PipelineRunIdentity BWaitsForA, PipelineRunIdentity BRunning, PipelineRunIdentity BWaitsForB)> Queued()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        var a = Run("Orders", TenantA);
        var bWaitsForA = Run("Refunds", TenantB);
        var bRunning = Run("Ledger", TenantB);
        var bWaitsForB = Run("Audit", TenantB);
        await scheduler.Enter(Claim(a, "store//t"), TestContext.Current.CancellationToken);
        scheduler.Queue(Claim(bWaitsForA, "store//t"));
        await scheduler.Enter(Claim(bRunning, "store//u"), TestContext.Current.CancellationToken);
        scheduler.Queue(Claim(bWaitsForB, "store//u"));
        return (scheduler, a, bWaitsForA, bRunning, bWaitsForB);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APlatformAdministratorReadsEveryTenantsRunsWithWhatHoldsEachBack()
    {
        var (scheduler, a, bWaitsForA, bRunning, _) = await Queued();
        var endpoint = Factory.Create<Queue>(Caller(platformAdmin: true), scheduler, Tenant(null));

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Running.Select(r => r.ExecutionId).ShouldBe([a.ExecutionId, bRunning.ExecutionId]);
        endpoint.Response.Waiting.Count.ShouldBe(2);
        var refunds = endpoint.Response.Waiting.Single(r => r.ExecutionId == bWaitsForA.ExecutionId);
        refunds.TenantId.ShouldBe(TenantB);
        refunds.WaitReason.ShouldBe("SharedTable");
        refunds.BlockedByExecutionId.ShouldBe(a.ExecutionId);
        refunds.Table.ShouldBe("store//t");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantReadsItsOwnRunsOnlyAndIsNotToldWhichRunOfAnotherTenantHoldsOneBack()
    {
        var (scheduler, _, bWaitsForA, bRunning, bWaitsForB) = await Queued();
        var endpoint = Factory.Create<Queue>(Caller(platformAdmin: false), scheduler, Tenant(TenantB));

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Running.ShouldHaveSingleItem().ExecutionId.ShouldBe(bRunning.ExecutionId);
        endpoint.Response.Waiting.Select(r => r.ExecutionId).ShouldBe([bWaitsForA.ExecutionId, bWaitsForB.ExecutionId]);
        var behindAnotherTenant = endpoint.Response.Waiting[0];
        behindAnotherTenant.WaitReason.ShouldBe("SharedTable");
        behindAnotherTenant.BlockedByExecutionId.ShouldBeNull();
        behindAnotherTenant.Table.ShouldBeNull();
        var behindItsOwn = endpoint.Response.Waiting[1];
        behindItsOwn.BlockedByExecutionId.ShouldBe(bRunning.ExecutionId);
        behindItsOwn.Table.ShouldBe("store//u");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnotherTenantSeesNothingOfATenantsRuns()
    {
        var (scheduler, _, _, _, _) = await Queued();
        var endpoint = Factory.Create<Queue>(Caller(platformAdmin: false), scheduler, Tenant(Guid.NewGuid()));

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Running.ShouldBeEmpty();
        endpoint.Response.Waiting.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARequestWithNoTenantFromSomeoneWhoDoesNotAdministerThePlatformIsRefused()
    {
        var (scheduler, _, _, _, _) = await Queued();
        var endpoint = Factory.Create<Queue>(Caller(platformAdmin: false), scheduler, Tenant(null));

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
    }
}
