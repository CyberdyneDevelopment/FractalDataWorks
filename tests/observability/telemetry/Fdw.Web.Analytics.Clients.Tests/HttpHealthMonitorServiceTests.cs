using System.Net;
using Fdw.Results;
using Fdw.Services.Abstractions.Health.Monitoring.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Analytics.Clients.Tests;

/// <summary>
/// The remote health monitor client answers a service the remote does not know as the same coded not found the local monitor answers
/// (<c>HealthMonitorLog.ServiceNotFound</c>, event 31000), so the service routes keep answering 404 for it; any other failed answer is
/// never a not found, and carries the HTTP status it came with as a detail and in its coded message (FDW-6466).
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "Health")]
public sealed class HttpHealthMonitorServiceTests
{
    private static string NotFoundCode => HealthMonitorLog.ServiceNotFound(NullLogger.Instance, "Etl").Code!;

    private static HttpHealthMonitorService Create(HttpStatusCode status)
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(status));
        var factory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factory.Setup(f => f.CreateClient("HealthMonitorClient"))
            .Returns(new HttpClient(handler) { BaseAddress = new Uri("https://monitor.example.com/") });
        return new HttpHealthMonitorService(factory.Object);
    }

    public static TheoryData<string> Routes => ["service", "history", "throughput"];

    private static async Task<IGenericResult> Ask(HttpHealthMonitorService sut, string route, CancellationToken ct) => route switch
    {
        "service" => await sut.GetServiceHealth("Etl", ct),
        "history" => await sut.GetHealthHistory("Etl", TimeSpan.FromHours(1), ct),
        _ => await sut.GetThroughput("Etl", TimeSpan.FromHours(1), ct),
    };

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task A404FromTheRemoteIsTheCodedNotFoundTheLocalMonitorAnswers(string route)
    {
        var result = await Ask(Create(HttpStatusCode.NotFound), route, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.Select(m => m.Code).ShouldContain(NotFoundCode);
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task A500FromTheRemoteIsNotANotFoundAndCarriesItsStatus(string route)
    {
        var result = await Ask(Create(HttpStatusCode.InternalServerError), route, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.Select(m => m.Code).ShouldNotContain(NotFoundCode);
        result.Messages.ShouldContain(m => m.Message.Contains("500", StringComparison.Ordinal));
        result.Details.ShouldNotBeNull().GetValue<int>("upstreamStatus").ShouldBe(500);
    }

    [Fact]
    public async Task A401FromTheRemoteIsNotANotFoundEither()
    {
        var result = await Create(HttpStatusCode.Unauthorized).GetServiceHealth("Etl", TestContext.Current.CancellationToken);

        result.Messages.Select(m => m.Code).ShouldNotContain(NotFoundCode);
        result.Details.ShouldNotBeNull().GetValue<int>("upstreamStatus").ShouldBe(401);
    }
}
