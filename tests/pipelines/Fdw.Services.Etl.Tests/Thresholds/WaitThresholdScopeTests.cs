using System.Collections.Generic;
using System.Linq;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Thresholds;
using Microsoft.Extensions.DependencyInjection;
using static Fdw.Services.Etl.Tests.Thresholds.WaitThresholdKit;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>
/// Tests that the singleton gate and reporter work in a scope of their own, under the tenant of the run or the subject (FDW-4611): the threshold source and the evaluator are scoped and
/// the evaluator hands its scope's services to the consequence handlers, so a singleton that held them would keep one set for every tenant for the life of the process, and a hold
/// check made in the flow of the trigger's caller would read the caller's tenant's thresholds, not the run's.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class WaitThresholdScopeTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    private static readonly Guid Caller = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    private static readonly Guid PipelineId = Guid.Parse("22222222-2222-4222-8222-222222222222");

    private readonly string? _before = EtlPipelineTypes.OperationalConnection;

    public void Dispose() => EtlPipelineTypes.OperationalConnection = _before;

    // Records the tenant the ambient accessor holds at the moment each call is made, and which instance made it.
    private sealed class Probe(IAuthenticationContextAccessor accessor) : IThresholdSource, IThresholdEvaluator
    {
        public static List<(string Call, Guid? Tenant, int Instance)> Calls { get; } = [];

        private static int _instances;

        private readonly int _instance = Interlocked.Increment(ref _instances);

        public Task<IGenericResult<IReadOnlyList<IThresholdDefinition>>> For(ThresholdSubject subject, CancellationToken cancellationToken = default)
        {
            Calls.Add(("For", accessor.Current?.ActiveTenantId, _instance));
            return Task.FromResult(GenericResult<IReadOnlyList<IThresholdDefinition>>.Success([]));
        }

        public Task<IGenericResult<ThresholdReport>> Report(ThresholdSubject subject, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default)
        {
            Calls.Add(("Report", accessor.Current?.ActiveTenantId, _instance));
            return Task.FromResult(GenericResult<ThresholdReport>.Success(new ThresholdReport(0, [], [])));
        }

        public Task<IGenericResult<ThresholdReport>> Judge(
            ThresholdSubject subject, IReadOnlyList<IThresholdDefinition> thresholds, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IReadOnlyList<ThresholdMatch> Holds(
            ThresholdSubject subject, IReadOnlyList<IThresholdDefinition> thresholds, IReadOnlyDictionary<string, object?> values, string escalation)
        {
            Calls.Add(("Holds", accessor.Current?.ActiveTenantId, _instance));
            return [];
        }
    }

    private static IServiceScopeFactory ProbeScopes(IAuthenticationContextAccessor accessor)
    {
        Probe.Calls.Clear();
        return Scopes(accessor, services =>
        {
            services.AddScoped<IThresholdSource>(sp => new Probe(sp.GetRequiredService<IAuthenticationContextAccessor>()));
            services.AddScoped<IThresholdEvaluator>(sp => new Probe(sp.GetRequiredService<IAuthenticationContextAccessor>()));
        });
    }

    private static PipelineExecutionRequest Request(Guid tenant) => new()
    {
        ExecutionId = Guid.NewGuid(), PipelineId = PipelineId, PipelineName = "Load", TriggerSource = "Api", TenantId = tenant, RunRoles = ["DataWriter"],
    };

    private static PipelineRunQueueEntry Waiting(Guid tenant) =>
        new(new PipelineRunIdentity(Guid.NewGuid(), PipelineId, "Load", tenant), DateTimeOffset.UnixEpoch, null, null);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task EachCheckReadsTheThresholdsOfTheTenantOfItsRunWhateverTenantTheCallerActsIn()
    {
        var accessor = new AuthenticationContextAccessor { Current = new SystemAuthenticationContext(Caller) };
        var gate = new WaitHoldGate(ProbeScopes(accessor));
        var owner = Layer("Server", "Own");
        var snapshot = new PipelineRunQueueSnapshot([], []);

        await gate.Check(Request(TenantA), owner, snapshot, TestContext.Current.CancellationToken);
        await gate.Check(Request(TenantB), owner, snapshot, TestContext.Current.CancellationToken);

        Probe.Calls.Where(call => call.Call == "For").Select(call => call.Tenant).ShouldBe([TenantA, TenantB]);
        Probe.Calls.Where(call => call.Call == "Holds").Select(call => call.Tenant).ShouldBe([TenantA, TenantB]);
        accessor.Current!.ActiveTenantId.ShouldBe(Caller);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task EachCheckResolvesItsOwnSourceAndEvaluator()
    {
        var gate = new WaitHoldGate(ProbeScopes(new AuthenticationContextAccessor()));
        var owner = Layer("Server", "Own");
        var snapshot = new PipelineRunQueueSnapshot([], []);

        await gate.Check(Request(TenantA), owner, snapshot, TestContext.Current.CancellationToken);
        await gate.Check(Request(TenantB), owner, snapshot, TestContext.Current.CancellationToken);

        // Two checks, four scoped objects: a gate that kept one of each would show two instances.
        Probe.Calls.Select(call => call.Instance).Distinct().Count().ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task EachSubjectIsReportedByAnEvaluatorOfItsOwnTenantsScope()
    {
        var accessor = new AuthenticationContextAccessor { Current = new SystemAuthenticationContext(Caller) };
        var reporter = new WaitThresholdReporter(ProbeScopes(accessor));

        await reporter.Report(new PipelineRunQueueSnapshot([], [Waiting(TenantA), Waiting(TenantB)]), TestContext.Current.CancellationToken);

        var reports = Probe.Calls.Where(call => call.Call == "Report").ToList();
        reports.Select(call => call.Tenant).Distinct().Order().ShouldBe(new[] { TenantA, TenantB }.Order().Select(t => (Guid?)t));
        reports.Count.ShouldBe(4);   // the pipeline and the server, for each of the two tenants
        reports.Select(call => call.Instance).Distinct().Count().ShouldBe(4);
        accessor.Current!.ActiveTenantId.ShouldBe(Caller);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheMonitorResolvesInAContainerThatRefusesASingletonThatHoldsAScopedService()
    {
        EtlPipelineTypes.OperationalConnection = null;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAuthenticationContextAccessor, AuthenticationContextAccessor>();
        services.AddScoped<IThresholdSource>(sp => new Probe(sp.GetRequiredService<IAuthenticationContextAccessor>()));
        services.AddScoped<IThresholdEvaluator>(sp => new Probe(sp.GetRequiredService<IAuthenticationContextAccessor>()));
        EtlPipelineTypes.RegisterPipelineExecutionQueue(services);

        // ValidateScopes makes the container refuse a singleton that takes a scoped service; the monitor takes the gate and the reporter, so it is captive through them
        // unless they take the scope factory.
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        provider.GetRequiredService<WaitThresholdMonitor>().ShouldNotBeNull();
        provider.GetRequiredService<WaitHoldGate>().ShouldNotBeNull();
        provider.GetRequiredService<WaitThresholdReporter>().ShouldNotBeNull();
    }
}
