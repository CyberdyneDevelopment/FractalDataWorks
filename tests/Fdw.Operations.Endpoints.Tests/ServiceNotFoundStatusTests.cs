using Fdw.Results;
using Fdw.Services.Abstractions.Health.Monitoring.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests;

/// <summary>
/// The health history and throughput routes answer a failed monitor result through <see cref="ResultHttpStatusMapper"/>
/// (FDW-2337). The status is read from the number of the failure's message, so a service the monitor does not know,
/// logged as <see cref="HealthMonitorLog.ServiceNotFound"/>, has to land in the Missing category and answer 404.
/// </summary>
[Trait("Priority", "P1")]
public class ServiceNotFoundStatusTests
{
    [Fact]
    public void A_service_the_monitor_does_not_know_answers_404()
    {
        var result = GenericResult.Failure(HealthMonitorLog.ServiceNotFound(NullLogger.Instance, "no-such-service"));

        var (status, _) = ResultHttpStatusMapper.Map(result, new DefaultHttpContext());

        status.ShouldBe(404);
    }
}
