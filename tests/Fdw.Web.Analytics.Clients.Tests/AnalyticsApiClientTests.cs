using System.Net;
using System.Net.Http.Json;
using Fdw.Web.Analytics.Clients.ApiClients;
using Fdw.Web.Analytics.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.Analytics.Clients.Tests;

public sealed class AnalyticsApiClientTests
{
    private static AnalyticsApiClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.example.com/") };
        return new AnalyticsApiClient(httpClient, Mock.Of<ILogger<AnalyticsApiClient>>());
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetAnalyticsSendsCorrectRequest()
    {
        var expected = new AnalyticsResponse
        {
            Summary = new AnalyticsSummary { TotalExecutions = 100 }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);
        var request = new AnalyticsRequest();

        var result = await sut.GetAnalytics(request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldStartWith("/analytics?startDate=");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Summary.TotalExecutions.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetAnalyticsReturnsFailureOnNonSuccess()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest));
        var sut = CreateClient(handler);
        var request = new AnalyticsRequest();

        var result = await sut.GetAnalytics(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetAnalyticsReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network error"));
        var sut = CreateClient(handler);
        var request = new AnalyticsRequest();

        var result = await sut.GetAnalytics(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetTopCalculationsSendsCorrectRequest()
    {
        var expected = new TopCalculationsResponse
        {
            Calculations = [new CalculationTypeStats { CalculationType = "Sum", ExecutionCount = 50 }]
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);
        var request = new TopCalculationsRequest { Count = 5 };

        var result = await sut.GetTopCalculations(request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldStartWith("/analytics/top-calculations?count=");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Calculations.Count.ShouldBe(1);
        result.Value.Calculations[0].CalculationType.ShouldBe("Sum");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetTopCalculationsReturnsFailureOnNonSuccess()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var sut = CreateClient(handler);
        var request = new TopCalculationsRequest();

        var result = await sut.GetTopCalculations(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetTopCalculationsReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network error"));
        var sut = CreateClient(handler);
        var request = new TopCalculationsRequest();

        var result = await sut.GetTopCalculations(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }
}
