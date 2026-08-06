using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions.Results;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// Tests for <see cref="ConnectionHealthMonitorWorker"/>'s handling of a configuration store that
/// registers no connection container — the normal shape for a host that manages zero connections
/// (e.g. a FileSystem-gateway client whose only connection is the bootstrap one in
/// configurationSchema.json). Such a store answering "this container does not exist here" is a stable
/// structural property of the host, not a failure, so the worker must state it once at Information and
/// stop rather than log an Error every scan tick forever.
/// </summary>
public sealed class ConnectionHealthMonitorWorkerTests
{
    private const int LoadConnectionsFailedEventId = 12202;
    private const int MonitoringIdleEventId = 12214;

    // ── Fakes ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Substitutes the provider's gateway-backed all-items read so the worker can be driven with an
    /// exact load result. The base constructor only stores the Lazy gateway, so a gateway that would
    /// throw on access is safe here: overriding Get means it is never dereferenced.
    /// </summary>
    private sealed class StubConnectionConfigurationProvider : ConnectionConfigurationProvider
    {
        private readonly IGenericResult<IReadOnlyList<ConnectionConfiguration>> _result;

        public StubConnectionConfigurationProvider(IGenericResult<IReadOnlyList<ConnectionConfiguration>> result)
            : base(
                NullLogger<ConnectionConfigurationProvider>.Instance,
                new Lazy<IConfigurationGateway>(static () => throw new InvalidOperationException(
                    "The stub provider overrides Get, so the gateway must never be resolved.")))
        {
            _result = result;
        }

        public int GetCallCount { get; private set; }

        public override Task<IGenericResult<IReadOnlyList<ConnectionConfiguration>>> Get(CancellationToken ct = default)
        {
            GetCallCount++;
            return Task.FromResult(_result);
        }
    }

    private sealed record LogEntry(LogLevel Level, int EventId);

    private sealed class RecordingLogger : ILogger<ConnectionHealthMonitorWorker>
    {
        private readonly List<LogEntry> _entries = [];

        public IReadOnlyList<LogEntry> Entries
        {
            get
            {
                lock (_entries) return _entries.ToArray();
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (_entries) _entries.Add(new LogEntry(logLevel, eventId.Id));
        }

        public int CountOf(int eventId)
        {
            var count = 0;
            foreach (var entry in Entries)
            {
                if (entry.EventId == eventId) count++;
            }

            return count;
        }

        /// <summary>
        /// Waits for <paramref name="eventId"/> to be logged at least once. Needed because
        /// BackgroundService.StartAsync only runs ExecuteAsync up to its first incomplete await — whether
        /// the startup sweep has reached its logging point by the time StartAsync returns is an
        /// implementation detail no assertion should depend on.
        /// </summary>
        public async Task WaitFor(int eventId, CancellationToken ct)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));

            while (CountOf(eventId) == 0)
            {
                timeout.Token.ThrowIfCancellationRequested();
                await Task.Delay(10, timeout.Token);
            }
        }
    }

    // ── Result builders ─────────────────────────────────────────────────────

    // Why this exact shape: it mirrors what DataStore.Path builds when the store registers no 'conn'
    // path — the typed code chained over the node's own navigation message — which ConfigurationGateway
    // and DefaultConfigurationProvider then propagate with ToNewResult (Code/InnerResult preserved).
    private static IGenericResult<IReadOnlyList<ConnectionConfiguration>> PathNotRegistered() =>
        GenericResult<IReadOnlyList<ConnectionConfiguration>>.Chain(
            DataStoresResultCodes.DataPathNotFound,
            GenericResult.Failure(new GenericMessage("Path 'conn' not found in DataStore 'ConfigurationDb'")),
            ResultDetails.Create("PathName", "conn", "DataStoreName", "ConfigurationDb"));

    private static IGenericResult<IReadOnlyList<ConnectionConfiguration>> ContainerNotRegistered() =>
        GenericResult<IReadOnlyList<ConnectionConfiguration>>.Chain(
            DataStoresResultCodes.ContainerNotFoundInPath,
            GenericResult.Failure(new GenericMessage("Container 'Connection' not found in path 'conn'")),
            ResultDetails.Create("ContainerName", "Connection", "PathName", "conn", "DataStoreName", "ConfigurationDb"));

    // Why message-only: a genuine transient failure (dropped connection, timeout, malformed row) carries
    // no result code — this is the shape that must KEEP the per-tick Error.
    private static IGenericResult<IReadOnlyList<ConnectionConfiguration>> TransientFailure() =>
        GenericResult<IReadOnlyList<ConnectionConfiguration>>.Failure(
            new GenericMessage("A network-related or instance-specific error occurred"));

    private static (ConnectionHealthMonitorWorker Worker, RecordingLogger Logger, StubConnectionConfigurationProvider Provider) CreateWorker(
        IGenericResult<IReadOnlyList<ConnectionConfiguration>> loadResult)
    {
        var provider = new StubConnectionConfigurationProvider(loadResult);
        var services = new ServiceCollection();

        // Why the explicit service type: the worker resolves ConnectionConfigurationProvider, so
        // registering the stub under its own derived type would leave that resolution unsatisfied.
        services.AddSingleton<ConnectionConfigurationProvider>(provider);

        var logger = new RecordingLogger();
        return (new ConnectionHealthMonitorWorker(
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(), logger), logger, provider);
    }

    // ── Absent container: state it once, then stop ──────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteWhenConnectionPathNotRegisteredLogsIdleOnceAndStops()
    {
        var (worker, logger, _) = CreateWorker(PathNotRegistered());

        await worker.StartAsync(TestContext.Current.CancellationToken);
        await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        logger.CountOf(MonitoringIdleEventId).ShouldBe(1);
        logger.CountOf(LoadConnectionsFailedEventId).ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteWhenConnectionContainerNotRegisteredLogsIdleOnceAndStops()
    {
        var (worker, logger, _) = CreateWorker(ContainerNotRegistered());

        await worker.StartAsync(TestContext.Current.CancellationToken);
        await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        logger.CountOf(MonitoringIdleEventId).ShouldBe(1);
        logger.CountOf(LoadConnectionsFailedEventId).ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteWhenConnectionPathNotRegisteredLogsIdleAtInformation()
    {
        var (worker, logger, _) = CreateWorker(PathNotRegistered());

        await worker.StartAsync(TestContext.Current.CancellationToken);
        await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        logger.Entries.ShouldContain(entry =>
            entry.EventId == MonitoringIdleEventId && entry.Level == LogLevel.Information);
        logger.Entries.ShouldNotContain(entry => entry.Level >= LogLevel.Error);
    }

    // Why this assertion matters most: the defect was UNBOUNDED repetition. Stopping the loop means the
    // store is read exactly once — never re-read every ScanTick for a condition that cannot change.
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteWhenConnectionPathNotRegisteredDoesNotReReadTheStore()
    {
        var (worker, _, provider) = CreateWorker(PathNotRegistered());

        await worker.StartAsync(TestContext.Current.CancellationToken);
        await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        provider.GetCallCount.ShouldBe(1);
    }

    // ── Genuine failure: unchanged fail-loud behaviour ──────────────────────

    // Why: this is the reference-api regression guard — a load that genuinely fails carries no result
    // code, must still log the per-tick Error, and must NOT be mistaken for a host that manages
    // zero connections (the worker keeps monitoring so the next tick retries).
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteWhenLoadFailsWithoutResultCodeStillLogsErrorAndKeepsMonitoring()
    {
        var (worker, logger, _) = CreateWorker(TransientFailure());

        await worker.StartAsync(TestContext.Current.CancellationToken);
        await logger.WaitFor(LoadConnectionsFailedEventId, TestContext.Current.CancellationToken);

        logger.CountOf(MonitoringIdleEventId).ShouldBe(0);
        worker.ExecuteTask!.IsCompleted.ShouldBeFalse();

        await worker.StopAsync(TestContext.Current.CancellationToken);
    }

    // ── Healthy host with nothing due: neither branch fires ─────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task ExecuteWhenStoreRegistersConnectionContainerKeepsMonitoring()
    {
        var (worker, logger, _) = CreateWorker(
            GenericResult<IReadOnlyList<ConnectionConfiguration>>.Success([]));

        await worker.StartAsync(TestContext.Current.CancellationToken);

        logger.CountOf(MonitoringIdleEventId).ShouldBe(0);
        logger.CountOf(LoadConnectionsFailedEventId).ShouldBe(0);
        worker.ExecuteTask!.IsCompleted.ShouldBeFalse();

        await worker.StopAsync(TestContext.Current.CancellationToken);
    }
}
