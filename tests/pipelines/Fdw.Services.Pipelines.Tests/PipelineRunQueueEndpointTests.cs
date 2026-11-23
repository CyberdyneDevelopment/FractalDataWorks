using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Settings;
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

    private sealed class Queue(PipelineRunScheduler scheduler, RunningFlows running) : ListPipelineRunQueueEndpointBase(NullLogger.Instance, scheduler, running);

    private static PipelineRunIdentity Run(string pipeline, Guid? tenant) => new(Guid.NewGuid(), Guid.NewGuid(), pipeline, tenant);

    private static PipelineRunClaim Claim(PipelineRunIdentity run, params string[] tables) => PipelineRunClaim.Writing(run, tables, Limits, null);

    // The refresh interval and, since FDW-5242, whether run events reach the hub; a null value is a setting that is not set.
    private sealed class Settings(int? seconds, bool? relay) : IEffectiveSettingsProvider
    {
        public T GetEffectiveValue<T>(string settingName, Guid? tenantId = null, string? roleName = null) => throw new NotSupportedException();

        public bool TryGetEffectiveValue<T>(string settingName, out T value, Guid? tenantId = null, string? roleName = null)
        {
            if (string.Equals(settingName, SettingDefinitions.PipelineEventRelayEnabled, StringComparison.Ordinal))
            {
                value = relay is { } on ? (T)(object)on : default!;
                return relay is not null;
            }

            value = seconds is { } set && settingName == SettingDefinitions.LiveViewRefreshSeconds ? (T)(object)set : default!;
            return seconds is not null && settingName == SettingDefinitions.LiveViewRefreshSeconds;
        }
    }

    // The tenant is the one the token states, the way every host has it (FDW-6977): the ETL host resolves no tenant per request, so no tenant context stands in for it here.
    private static Action<DefaultHttpContext> Caller(bool platformAdmin, int? refreshSeconds = 2, bool? relay = false, Guid? tenant = null) => ctx =>
    {
        var claims = new List<Claim> { new("sub", "caller") };
        if (tenant is { } own)
            claims.Add(new Claim(ClaimDefinitions.tenantId.Name, own.ToString("D")));

        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var roles = new Mock<ISystemRoleConfiguration>();
        roles.Setup(r => r.IsPlatformAdmin(It.IsAny<ClaimsPrincipal>())).Returns(platformAdmin);
        roles.SetupGet(r => r.AdminRoleName).Returns("Admin");
        ctx.AddTestServices(s =>
        {
            s.AddSingleton(roles.Object);
            s.AddSingleton<IEffectiveSettingsProvider>(new Settings(refreshSeconds, relay));
        });
    };

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
        var endpoint = Factory.Create<Queue>(Caller(platformAdmin: true), scheduler, new RunningFlows());

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
        var endpoint = Factory.Create<Queue>(Caller(platformAdmin: false, tenant: TenantB), scheduler, new RunningFlows());

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
    public async Task ARunInFlightThatTheHostFollowsListsItsComponentsAndTheOthersListNone()
    {
        var (scheduler, a, bWaitsForA, bRunning, _) = await Queued();
        var flows = new RunningFlows();
        using var live = flows.Begin(a.ExecutionId);
        using var graph = live.Open([new FlowLiveComponent("src", 1, true), new FlowLiveComponent("dst", 2, false)], () => []);
        graph.ComponentStarted("src");
        var endpoint = Factory.Create<Queue>(Caller(platformAdmin: true), scheduler, flows);

        await endpoint.HandleAsync(CancellationToken.None);

        var followed = endpoint.Response.Running.Single(r => r.ExecutionId == a.ExecutionId);
        followed.Components.ShouldNotBeNull();
        followed.Components!.Select(c => c.Name).ShouldBe(["src", "dst"], ignoreOrder: true);
        var src = followed.Components.Single(c => c.Name == "src");
        src.Stage.ShouldBe(1);
        src.StartedAt.ShouldNotBeNull();
        src.EndedAt.ShouldBeNull();
        var dst = followed.Components.Single(c => c.Name == "dst");
        dst.StartedAt.ShouldBeNull();
        dst.Rows.ShouldBeNull();
        endpoint.Response.Running.Single(r => r.ExecutionId == bRunning.ExecutionId).Components.ShouldBeNull();
        endpoint.Response.Waiting.Single(r => r.ExecutionId == bWaitsForA.ExecutionId).Components.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TheAnswerCarriesTheRefreshIntervalTheSettingGivesTheCaller()
    {
        var (scheduler, _, _, _, _) = await Queued();
        var endpoint = Factory.Create<Queue>(Caller(platformAdmin: true, refreshSeconds: 5), scheduler, new RunningFlows());

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.Response.RefreshSeconds.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASettingThatIsNotSetAnswersNoIntervalAndAssumesNone()
    {
        var (scheduler, _, _, _, _) = await Queued();
        var endpoint = Factory.Create<Queue>(Caller(platformAdmin: true, refreshSeconds: null), scheduler, new RunningFlows());

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.RefreshSeconds.ShouldBeNull();
        endpoint.Response.Running.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnIntervalThatIsNotPositiveIsNotAnswered()
    {
        var (scheduler, _, _, _, _) = await Queued();
        var endpoint = Factory.Create<Queue>(Caller(platformAdmin: true, refreshSeconds: 0), scheduler, new RunningFlows());

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.Response.RefreshSeconds.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnotherTenantSeesNothingOfATenantsRuns()
    {
        var (scheduler, _, _, _, _) = await Queued();
        var endpoint = Factory.Create<Queue>(Caller(platformAdmin: false, tenant: Guid.NewGuid()), scheduler, new RunningFlows());

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
        var endpoint = Factory.Create<Queue>(Caller(platformAdmin: false), scheduler, new RunningFlows());

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "FDW5242")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TheAnswerSaysWhetherRunEventsReachTheHub(bool relay)
    {
        var (scheduler, _, _, _, _) = await Queued();
        var endpoint = Factory.Create<Queue>(Caller(platformAdmin: true, relay: relay), scheduler, new RunningFlows());

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.RelayEnabled.ShouldBe(relay);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "FDW5242")]
    public async Task ARelaySettingThatCannotBeReadRefusesTheRequestAndIsNeverTakenAsOff()
    {
        var (scheduler, _, _, _, _) = await Queued();
        var endpoint = Factory.Create<Queue>(Caller(platformAdmin: true, relay: null), scheduler, new RunningFlows());

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(500);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "FDW5232")]
    public async Task ARunInFlightCarriesWhatWaitsInItsLinksWhatItsComponentsHoldAndItsTotalsAtOneSampleTime()
    {
        var (scheduler, a, bWaitsForA, _, _) = await Queued();
        var flows = new RunningFlows();
        using var live = flows.Begin(a.ExecutionId);
        using var graph = live.Open(
            [new FlowLiveComponent("src", 1, true), new FlowLiveComponent("dst", 2, false)],
            () => [new FlowLinkCount(new FlowLink("src", "output", "dst", "input"), 10, 2, 400) { QueuedBytes = 120, QueuedBatches = 1, Blocked = TimeSpan.FromMilliseconds(30) }],
            name => string.Equals(name, "dst", StringComparison.OrdinalIgnoreCase) ? new FlowHeldCount(300, 3, 500) : new FlowHeldCount(0, 0, 0));
        graph.ComponentStarted("src");
        graph.ComponentStarted("dst");
        var endpoint = Factory.Create<Queue>(Caller(platformAdmin: true), scheduler, flows);

        await endpoint.HandleAsync(CancellationToken.None);

        var run = endpoint.Response.Running.Single(r => r.ExecutionId == a.ExecutionId);
        run.SampledAt.ShouldNotBeNull();
        run.QueuedBytes.ShouldBe(120);
        run.HeldBytes.ShouldBe(300);
        run.TotalBytes.ShouldBe(420);
        run.BytesMeasure.ShouldBe("EstimatedValueBytes");
        var link = run.Links.ShouldNotBeNull().ShouldHaveSingleItem();
        (link.Bytes, link.Batches, link.QueuedBytes, link.QueuedBatches, link.BlockedMs).ShouldBe((400L, 2L, 120L, 1L, 30L));
        var components = run.Components.ShouldNotBeNull();
        components.Single(c => c.Name == "dst").HeldBytes.ShouldBe(300);
        components.Single(c => c.Name == "dst").PeakHeldBytes.ShouldBe(500);
        components.Single(c => c.Name == "src").BlockedMs.ShouldBe(30, "a component waits on the links it writes");
        endpoint.Response.Waiting.Single(r => r.ExecutionId == bWaitsForA.ExecutionId).SampledAt.ShouldBeNull("a run that waits has no figures");
    }
}
