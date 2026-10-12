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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// One connection that cannot answer must never stop the others being checked (FDW-928). Both the
/// monitor worker and the Connections health check probe connection after connection; a probe or a
/// write that threw used to end the pass, leaving every later connection unchecked.
/// </summary>
public sealed class ConnectionHealthIsolationTests
{
    private static Mock<IConnectionImplementationConfiguration> Connection(string name)
    {
        var connection = new Mock<IConnectionImplementationConfiguration>();
        connection.SetupGet(c => c.Id).Returns(Guid.NewGuid());
        connection.SetupGet(c => c.Name).Returns(name);
        connection.SetupGet(c => c.HealthCheckEnabled).Returns(true);
        connection.SetupGet(c => c.HealthCheckOnStartup).Returns(true);
        return connection;
    }

    private static IGenericConnection Probing(Func<Task<IGenericResult>> probe)
    {
        var connection = new Mock<IGenericConnection>();
        connection.As<ISupportsHealthProbe>().Setup(p => p.Probe(It.IsAny<CancellationToken>())).Returns(probe);
        return connection.Object;
    }

    /// <summary>
    /// "Broken" throws from its probe; "Working" answers. The provider serves them in that order, so
    /// Working is only checked if Broken did not end the pass.
    /// </summary>
    private static (IServiceProvider Services, Mock<IConnectionImplementationConfiguration> Broken,
        Mock<IConnectionImplementationConfiguration> Working, Mock<IConnectionHealthService> Health) TwoConnections()
    {
        var broken = Connection("Broken");
        var working = Connection("Working");

        // Both the worker and the health check read the rows alone, and each builds the connections it
        // checks by Id (the worker) or by name (the health check), which reads them whole (FDW-970).
        var configurations = new Mock<IConnectionConfigurationProvider>();
        configurations
            .Setup(p => p.Get(ConfigurationLevels.RecordOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IConnectionImplementationConfiguration>>.Success(
                new[] { broken.Object, working.Object }));

        var brokenConnection = Probing(() => throw new InvalidOperationException("no base address"));
        var workingConnection = Probing(() => Task.FromResult(GenericResult.Success()));

        var connections = new Mock<IConnectionProvider>();
        connections
            .Setup(p => p.Get(broken.Object.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IGenericConnection>.Success(brokenConnection));
        connections
            .Setup(p => p.Get(working.Object.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IGenericConnection>.Success(workingConnection));
        connections
            .Setup(p => p.Get("Broken", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IGenericConnection>.Success(brokenConnection));
        connections
            .Setup(p => p.Get("Working", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IGenericConnection>.Success(workingConnection));

        var health = new Mock<IConnectionHealthService>();
        health
            .Setup(h => h.RecordHealthCheck(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var services = new ServiceCollection();
        services.AddSingleton(configurations.Object);
        services.AddSingleton(connections.Object);
        services.AddSingleton(health.Object);
        // The worker elevates every probe through this accessor, so a scope without one cannot run.
        services.AddSingleton<IAuthenticationContextAccessor, AuthenticationContextAccessor>();

        return (services.BuildServiceProvider(), broken, working, health);
    }

    private static int Recorded(Mock<IConnectionHealthService> health) =>
        health.Invocations.Count(i => string.Equals(i.Method.Name, nameof(IConnectionHealthService.RecordHealthCheck), StringComparison.Ordinal));

    private static async Task WaitForRecords(Mock<IConnectionHealthService> health, int count, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        while (Recorded(health) < count)
        {
            timeout.Token.ThrowIfCancellationRequested();
            await Task.Delay(10, timeout.Token);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task WorkerRecordsAThrowingProbeUnhealthyAndStillProbesTheNextConnection()
    {
        var (services, broken, working, health) = TwoConnections();
        var worker = new ConnectionHealthMonitorWorker(
            services.GetRequiredService<IServiceScopeFactory>(), TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(5),
            NullLogger<ConnectionHealthMonitorWorker>.Instance);

        await worker.StartAsync(TestContext.Current.CancellationToken);
        await WaitForRecords(health, 2, TestContext.Current.CancellationToken);

        health.Verify(h => h.RecordHealthCheck(broken.Object.Id, "Broken", false, null,
            It.Is<string?>(m => m != null && m.Contains("no base address", StringComparison.Ordinal)), It.IsAny<CancellationToken>()), Times.Once);
        health.Verify(h => h.RecordHealthCheck(working.Object.Id, "Working", true, It.IsAny<int?>(),
            null, It.IsAny<CancellationToken>()), Times.Once);
        worker.ExecuteTask!.IsCompleted.ShouldBeFalse();

        await worker.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task WorkerKeepsGoingWhenRecordingOneConnectionThrows()
    {
        var (services, broken, working, health) = TwoConnections();
        health
            .Setup(h => h.RecordHealthCheck(broken.Object.Id, It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("history store unavailable"));
        var worker = new ConnectionHealthMonitorWorker(
            services.GetRequiredService<IServiceScopeFactory>(), TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(5),
            NullLogger<ConnectionHealthMonitorWorker>.Instance);

        await worker.StartAsync(TestContext.Current.CancellationToken);
        await WaitForRecords(health, 2, TestContext.Current.CancellationToken);

        health.Verify(h => h.RecordHealthCheck(working.Object.Id, "Working", true, It.IsAny<int?>(),
            null, It.IsAny<CancellationToken>()), Times.Once);
        worker.ExecuteTask!.IsCompleted.ShouldBeFalse();

        await worker.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task HealthCheckReportsAThrowingProbeUnhealthyAndStillChecksTheRest()
    {
        var (services, _, _, _) = TwoConnections();

        var result = await new ConnectionsHealthCheckable(NullLogger<ConnectionsHealthCheckable>.Instance)
            .CheckHealth(services, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Status.Name.ShouldBe("Unhealthy");
        result.Value.Description.ShouldNotBeNull();
        result.Value.Description.ShouldBe("2 connection(s) checked: 1 healthy, 0 degraded, 1 unhealthy");
        // The description is served anonymously as health history (FDW-1098): no connection's name, and no probe's error.
        result.Value.Description.ShouldNotContain("Broken");
        result.Value.Description.ShouldNotContain("no base address");
    }
}
