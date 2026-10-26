using System;
using System.Threading.Tasks;
using Fdw.Services.Abstractions.Health;
using Fdw.Services.HealthChecks.Monitoring;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.HealthChecks;

/// <summary>
/// Whether a service exists is decided by the services the monitor holds, not by whether it has recorded a check yet
/// (FDW-2337). A registered service that has not been checked has an empty history, which is true; only a name no
/// registered service carries is not found.
/// </summary>
[Trait("Priority", "P1")]
public class HealthMonitorUncheckedServiceTests
{
    private static HealthMonitorService MonitorOf(string serviceName)
    {
        var checkable = new Mock<IHealthCheckable>();
        checkable.SetupGet(c => c.ServiceName).Returns(serviceName);

        return new HealthMonitorService(
            new[] { checkable.Object },
            new Mock<IServiceProvider>().Object,
            new LocalHealthMonitorConfiguration { Name = "Local", CheckIntervalSeconds = 60, HistoryRetentionMinutes = 60, ThroughputWindowSeconds = 60 },
            NullLogger<HealthMonitorService>.Instance);
    }

    [Fact]
    public async Task A_registered_service_that_has_not_been_checked_has_an_empty_history()
    {
        var result = await MonitorOf("Alpha").GetHealthHistory("Alpha", TimeSpan.FromHours(1), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_registered_service_that_has_not_been_checked_has_no_throughput_data_points()
    {
        var result = await MonitorOf("Alpha").GetThroughput("alpha", TimeSpan.FromHours(1), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.DataPoints.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_name_no_registered_service_carries_has_no_history()
    {
        var result = await MonitorOf("Alpha").GetHealthHistory("Beta", TimeSpan.FromHours(1), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task A_name_no_registered_service_carries_has_no_throughput()
    {
        var result = await MonitorOf("Alpha").GetThroughput("Beta", TimeSpan.FromHours(1), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
