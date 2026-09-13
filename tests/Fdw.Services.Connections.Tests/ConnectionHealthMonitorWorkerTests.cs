using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions.Results;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Data;

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
    /// The real provider over a faked store: it is sealed and its reads are not virtual, so the
    /// worker's configuration source is stated the way production states it -- what the gateway
    /// answers when the provider reads its domain rows.
    /// </summary>
    private static (ConnectionConfigurationProvider Provider, Mock<IConfigurationGateway> Gateway) ProviderOver(
        IGenericResult<IEnumerable<DomainConfiguration>> rows)
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<DomainConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways
            .Setup(p => p.Get(It.IsAny<string>()))
            .Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));

        return (
            new ConnectionConfigurationProvider(
                NullLogger<ConnectionConfigurationProvider>.Instance, gateways.Object, "PlatformConfiguration"),
            gateway);
    }

    /// <summary>How many times the store was read for the connection rows.</summary>
    private static int StoreReads(Mock<IConfigurationGateway> gateway) =>
        gateway.Invocations.Count(i => string.Equals(i.Method.Name, "Execute", StringComparison.Ordinal));

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

    private static IGenericResult<IEnumerable<DomainConfiguration>> PathNotRegistered() =>
        GenericResult<IEnumerable<DomainConfiguration>>.Chain(
            DataStoresResultCodes.DataPathNotFound,
            GenericResult.Failure(new GenericMessage("Path 'conn' not found in DataStore 'ConfigurationDb'")),
            ResultDetails.Create("PathName", "conn", "DataStoreName", "PlatformConfiguration"));

    private static IGenericResult<IEnumerable<DomainConfiguration>> ContainerNotRegistered() =>
        GenericResult<IEnumerable<DomainConfiguration>>.Chain(
            DataStoresResultCodes.ContainerNotFoundInPath,
            GenericResult.Failure(new GenericMessage("Container 'Connection' not found in path 'conn'")),
            ResultDetails.Create("ContainerName", "Connection", "PathName", "conn", "DataStoreName", "PlatformConfiguration"));

    private static IGenericResult<IEnumerable<DomainConfiguration>> TransientFailure() =>
        GenericResult<IEnumerable<DomainConfiguration>>.Failure(
            new GenericMessage("A network-related or instance-specific error occurred"));

    private static (ConnectionHealthMonitorWorker Worker, RecordingLogger Logger, Mock<IConfigurationGateway> Gateway) CreateWorker(
        IGenericResult<IEnumerable<DomainConfiguration>> loadResult)
    {
        var (provider, gateway) = ProviderOver(loadResult);
        var services = new ServiceCollection();

        services.AddSingleton<ConnectionConfigurationProvider>(provider);
        services.AddSingleton<IConnectionConfigurationProvider>(sp => sp.GetRequiredService<ConnectionConfigurationProvider>());

        var logger = new RecordingLogger();
        return (new ConnectionHealthMonitorWorker(
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(), logger), logger, gateway);
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

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteWhenConnectionPathNotRegisteredDoesNotReReadTheStore()
    {
        var (worker, _, gateway) = CreateWorker(PathNotRegistered());

        await worker.StartAsync(TestContext.Current.CancellationToken);
        await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        StoreReads(gateway).ShouldBe(1);
    }

    // ── Genuine failure: unchanged fail-loud behaviour ──────────────────────

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
            GenericResult<IEnumerable<DomainConfiguration>>.Success([]));

        await worker.StartAsync(TestContext.Current.CancellationToken);

        logger.CountOf(MonitoringIdleEventId).ShouldBe(0);
        logger.CountOf(LoadConnectionsFailedEventId).ShouldBe(0);
        worker.ExecuteTask!.IsCompleted.ShouldBeFalse();

        await worker.StopAsync(TestContext.Current.CancellationToken);
    }
}
