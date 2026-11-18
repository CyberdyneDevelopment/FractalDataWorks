using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Messages;
using Fdw.Operations.Endpoints.Health;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Tests.Health;

/// <summary>
/// GET /health/system names why the HealthMonitor read failed (FDW-6466). A read that failed is answered with its own message and code,
/// and the endpoint's log line carries them. "Does not know which monitor to report to" is said only when the read succeeded and named
/// none, or named several.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "Health")]
public sealed class GetSystemHealthMonitorReadTests
{
    private const string ReadCode = "MSSQL-70229";
    private const string ReadText = "SQL permission denied on SELECT of hlth.HealthMonitor";

    private sealed class Probe(
        IHealthMonitorProvider monitors,
        IHealthMonitorConfigurationProvider configurations,
        ILogger<GetSystemHealthEndpointBase> logger)
        : GetSystemHealthEndpointBase(monitors, configurations, logger);

    private sealed class ServiceProbe(
        IHealthMonitorProvider monitors,
        IHealthMonitorConfigurationProvider configurations,
        ILogger<GetServiceHealthEndpointBase> logger)
        : GetServiceHealthEndpointBase(monitors, configurations, logger);

    private sealed class FakeAccessor : IAuthenticationContextAccessor
    {
        public IAuthenticationContext? Current { get; set; }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Lines { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Lines.Add(formatter(state, exception));
    }

    private static (Probe Endpoint, CapturingLogger<GetSystemHealthEndpointBase> Log, Mock<IHealthMonitorProvider> Monitors) Create(
        IGenericResult<IReadOnlyList<IHealthMonitorImplementationConfiguration>> read)
    {
        var configurations = new Mock<IHealthMonitorConfigurationProvider>(MockBehavior.Strict);
        configurations.Setup(c => c.Get(It.IsAny<CancellationToken>())).ReturnsAsync(read);
        var monitors = new Mock<IHealthMonitorProvider>(MockBehavior.Strict);
        var log = new CapturingLogger<GetSystemHealthEndpointBase>();
        var endpoint = Factory.Create<Probe>(
            ctx =>
            {
                ctx.Response.Body = new MemoryStream();
                ctx.AddTestServices(s => s.AddSingleton<IAuthenticationContextAccessor>(new FakeAccessor()));
            },
            monitors.Object,
            configurations.Object,
            log);
        return (endpoint, log, monitors);
    }

    private static string Body(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return new StreamReader(context.Response.Body).ReadToEnd();
    }

    private static IHealthMonitorImplementationConfiguration Row(string name)
    {
        var row = new Mock<IHealthMonitorImplementationConfiguration>();
        row.SetupGet(r => r.Name).Returns(name);
        return row.Object;
    }

    [Fact]
    public async Task AReadThatFailedIsAnsweredWithItsOwnCodeAndTheMonitorIsNeverAsked()
    {
        var failed = GenericResult<IReadOnlyList<IHealthMonitorImplementationConfiguration>>.Failure(
            new GenericMessage(MessageSeverity.Error, ReadText, ReadCode));
        var (endpoint, _, monitors) = Create(failed);

        await endpoint.HandleAsync(TestContext.Current.CancellationToken);

        var (expectedStatus, _) = ResultHttpStatusMapper.Map(failed, new DefaultHttpContext());
        endpoint.HttpContext.Response.StatusCode.ShouldBe(expectedStatus);
        Body(endpoint.HttpContext).ShouldContain(ReadCode);
        monitors.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AReadThatFailedIsLoggedWithItsOwnTextAndCode()
    {
        var failed = GenericResult<IReadOnlyList<IHealthMonitorImplementationConfiguration>>.Failure(
            new GenericMessage(MessageSeverity.Error, ReadText, ReadCode));
        var (endpoint, log, _) = Create(failed);

        await endpoint.HandleAsync(TestContext.Current.CancellationToken);

        log.Lines.ShouldContain(line => line.Contains(ReadText, StringComparison.Ordinal) && line.Contains(ReadCode, StringComparison.Ordinal));
    }

    [Fact]
    public async Task AReadThatFailedIsNotAnsweredAsAHostThatDoesNotKnowWhichMonitorToReportTo()
    {
        var failed = GenericResult<IReadOnlyList<IHealthMonitorImplementationConfiguration>>.Failure(
            new GenericMessage(MessageSeverity.Error, ReadText, ReadCode));
        var (endpoint, log, _) = Create(failed);

        await endpoint.HandleAsync(TestContext.Current.CancellationToken);

        Body(endpoint.HttpContext).ShouldNotContain("does not know which monitor");
        log.Lines.ShouldNotContain(line => line.Contains("does not know which monitor", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AReadThatSucceededAndNamedNoMonitorSaysTheHostDoesNotKnowWhichToReportTo()
    {
        var (endpoint, log, monitors) = Create(GenericResult<IReadOnlyList<IHealthMonitorImplementationConfiguration>>.Success([]));

        await endpoint.HandleAsync(TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        log.Lines.ShouldContain(line => line.Contains("does not know which monitor", StringComparison.Ordinal) && line.Contains("none", StringComparison.OrdinalIgnoreCase));
        monitors.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AReadThatSucceededAndNamedSeveralMonitorsSaysSoAndCountsThem()
    {
        var (endpoint, log, monitors) = Create(
            GenericResult<IReadOnlyList<IHealthMonitorImplementationConfiguration>>.Success([Row("Default"), Row("Second")]));

        await endpoint.HandleAsync(TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        log.Lines.ShouldContain(line => line.Contains("does not know which monitor", StringComparison.Ordinal) && line.Contains("2", StringComparison.Ordinal));
        monitors.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AServiceHealthRequestWhoseMonitorReadFailedIsAnsweredWithTheReadsCodeNotAsAServiceNotFound()
    {
        var failed = GenericResult<IReadOnlyList<IHealthMonitorImplementationConfiguration>>.Failure(
            new GenericMessage(MessageSeverity.Error, ReadText, ReadCode));
        var configurations = new Mock<IHealthMonitorConfigurationProvider>(MockBehavior.Strict);
        configurations.Setup(c => c.Get(It.IsAny<CancellationToken>())).ReturnsAsync(failed);
        var monitors = new Mock<IHealthMonitorProvider>(MockBehavior.Strict);
        var endpoint = Factory.Create<ServiceProbe>(
            ctx =>
            {
                ctx.Response.Body = new MemoryStream();
                ctx.AddTestServices(s => s.AddSingleton<IAuthenticationContextAccessor>(new FakeAccessor()));
            },
            monitors.Object,
            configurations.Object,
            new CapturingLogger<GetServiceHealthEndpointBase>());

        await endpoint.HandleAsync(new ServiceHealthRequest { Name = "Etl" }, TestContext.Current.CancellationToken);

        var (expectedStatus, _) = ResultHttpStatusMapper.Map(failed, new DefaultHttpContext());
        endpoint.HttpContext.Response.StatusCode.ShouldBe(expectedStatus);
        Body(endpoint.HttpContext).ShouldContain(ReadCode);
        monitors.VerifyNoOtherCalls();
    }

    private static (ServiceProbe Endpoint, Mock<IHealthMonitorService> Monitor) CreateServiceProbeWithAMonitorThatAnswers(
        IGenericResult<ServiceHealthSnapshot> health)
    {
        var configurations = new Mock<IHealthMonitorConfigurationProvider>(MockBehavior.Strict);
        configurations.Setup(c => c.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IHealthMonitorImplementationConfiguration>>.Success([Row("Default")]));
        var monitor = new Mock<IHealthMonitorService>(MockBehavior.Strict);
        monitor.Setup(m => m.GetServiceHealth("Etl", It.IsAny<CancellationToken>())).ReturnsAsync(health);
        var monitors = new Mock<IHealthMonitorProvider>(MockBehavior.Strict);
        monitors.Setup(m => m.Get("Default", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IHealthMonitorService>.Success(monitor.Object));
        var endpoint = Factory.Create<ServiceProbe>(
            ctx =>
            {
                ctx.Response.Body = new MemoryStream();
                ctx.AddTestServices(s => s.AddSingleton<IAuthenticationContextAccessor>(new FakeAccessor()));
            },
            monitors.Object,
            configurations.Object,
            new CapturingLogger<GetServiceHealthEndpointBase>());
        return (endpoint, monitor);
    }

    [Fact]
    public async Task AServiceHealthReadThatFailedIsAnsweredWithItsOwnCodeNotAsAServiceNotFound()
    {
        var failed = GenericResult<ServiceHealthSnapshot>.Failure(new GenericMessage(MessageSeverity.Error, "the health check could not run", "HEALTH-70777"));
        var (endpoint, _) = CreateServiceProbeWithAMonitorThatAnswers(failed);

        await endpoint.HandleAsync(new ServiceHealthRequest { Name = "Etl" }, TestContext.Current.CancellationToken);

        var (expectedStatus, _) = ResultHttpStatusMapper.Map(failed, new DefaultHttpContext());
        endpoint.HttpContext.Response.StatusCode.ShouldBe(expectedStatus);
        Body(endpoint.HttpContext).ShouldContain("HEALTH-70777");
    }

    [Fact]
    public async Task AServiceTheMonitorDoesNotKnowIsStillAnswered404ByTheMonitorsOwnNotFound()
    {
        var notFound = GenericResult<ServiceHealthSnapshot>.Failure(new GenericMessage(MessageSeverity.Warning, "Service 'Etl' not found in registered services", "HEALTH-31000"));
        var (endpoint, _) = CreateServiceProbeWithAMonitorThatAnswers(notFound);

        await endpoint.HandleAsync(new ServiceHealthRequest { Name = "Etl" }, TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
        Body(endpoint.HttpContext).ShouldContain("HEALTH-31000");
    }
}
