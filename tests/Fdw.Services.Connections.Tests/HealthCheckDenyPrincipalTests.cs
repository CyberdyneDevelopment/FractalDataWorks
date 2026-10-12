using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Abstractions.Health;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.HealthChecks.Monitoring;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// An anonymous health probe must not open connections as the deny principal (FDW-1098). It used to log event 11067
/// (MsSqlConnectionLogger.NoAccessPrincipalContextSet) once per connection on every probe, because its platform reads
/// ran in the anonymous request's context. The warning itself must still fire for a connection opened with no context.
/// </summary>
/// <remarks>
/// No SQL Server is involved. <see cref="Open"/> does what the MsSql connection does when it opens
/// (FdwReferenceConnections.MsSql.MsSqlConnection): choose the session context from the accessor's current context,
/// plan it and log that it was applied. That choice, and the line it logs, is what is under test.
/// </remarks>
public sealed class HealthCheckDenyPrincipalTests
{
    private const int DenyPrincipalEventId = 11067;

    private sealed class RecordingLogger : ILogger
    {
        private readonly List<(LogLevel Level, int EventId)> _entries = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => _entries.Add((logLevel, eventId.Id));

        public int CountOf(int eventId, LogLevel level) => _entries.Count(e => e.EventId == eventId && e.Level == level);
    }

    private static void Open(AuthenticationContextAccessor accessor, string database, ILogger logger, List<string> chosen)
    {
        var sessionContext = MsSqlSessionContextTypes.For(accessor.Current);
        chosen.Add(sessionContext.Name);
        sessionContext.LogApplied(
            sessionContext.Plan(accessor.Current), new SqlConnection($"Server=health-probe;Database={database}"), logger);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AnAnonymousHealthProbeOpensNoConnectionAsTheDenyPrincipal()
    {
        var accessor = new AuthenticationContextAccessor { Current = null };
        var logger = new RecordingLogger();
        var chosen = new List<string>();

        var opsDb = new Mock<IConnectionImplementationConfiguration>();
        opsDb.SetupGet(c => c.Id).Returns(Guid.NewGuid());
        opsDb.SetupGet(c => c.Name).Returns("OpsDb");
        opsDb.SetupGet(c => c.HealthCheckEnabled).Returns(true);

        // Every read and the probe open a connection, as they do against ConfigurationDb and the probed database.
        var configurations = new Mock<IConnectionConfigurationProvider>();
        configurations
            .Setup(p => p.Get(ConfigurationLevels.RecordOnly, It.IsAny<CancellationToken>()))
            .Callback(() => Open(accessor, "ConfigurationDb", logger, chosen))
            .ReturnsAsync(GenericResult<IReadOnlyList<IConnectionImplementationConfiguration>>.Success(new[] { opsDb.Object }));

        var probed = new Mock<IGenericConnection>();
        probed.As<ISupportsHealthProbe>()
            .Setup(p => p.Probe(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                Open(accessor, "OpsDb", logger, chosen);
                return Task.FromResult(GenericResult.Success());
            });

        var connections = new Mock<IConnectionProvider>();
        connections
            .Setup(p => p.Get("OpsDb", It.IsAny<CancellationToken>()))
            .Callback(() => Open(accessor, "ConfigurationDb", logger, chosen))
            .ReturnsAsync(GenericResult<IGenericConnection>.Success(probed.Object));

        var services = new ServiceCollection();
        services.AddSingleton(configurations.Object);
        services.AddSingleton(connections.Object);
        services.AddSingleton<IAuthenticationContextAccessor>(accessor);
        var provider = services.BuildServiceProvider();

        // The path GET /health/system takes: the monitor runs every checkable it holds.
        var monitor = new HealthMonitorService(
            new IHealthCheckable[] { new ConnectionsHealthCheckable(NullLogger<ConnectionsHealthCheckable>.Instance) },
            provider,
            new LocalHealthMonitorConfiguration { Name = "Local", CheckIntervalSeconds = 60, HistoryRetentionMinutes = 60, ThroughputWindowSeconds = 60 },
            NullLogger<HealthMonitorService>.Instance);

        var result = await monitor.GetSystemHealth(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.OverallStatus.Name.ShouldBe("Healthy");
        chosen.Count.ShouldBe(3, "the row read, the connection's resolve and its probe each open a connection");
        chosen.ShouldAllBe(name => string.Equals(name, "SystemContext", StringComparison.Ordinal));
        logger.CountOf(DenyPrincipalEventId, LogLevel.Warning).ShouldBe(0);
        accessor.Current.ShouldBeNull("the elevation ends with the check and never reaches the caller's request");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AConnectionOpenedWithNoContextStillWarnsAsTheDenyPrincipal()
    {
        var accessor = new AuthenticationContextAccessor { Current = null };
        var logger = new RecordingLogger();
        var chosen = new List<string>();

        Open(accessor, "ConfigurationDb", logger, chosen);

        chosen.Single().ShouldBe("Deny");
        logger.CountOf(DenyPrincipalEventId, LogLevel.Warning).ShouldBe(1);
    }
}
