using System.Net;
using System.Net.Http.Json;
using Fdw.Web.Analytics.Clients.ApiClients;
using Fdw.Web.Analytics.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.Analytics.Clients.Tests;

public sealed class ProfileApiClientTests
{
    private static ProfileApiClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.example.com/") };
        return new ProfileApiClient(httpClient, Mock.Of<ILogger<ProfileApiClient>>());
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetProfileSendsCorrectRequest()
    {
        var expected = new DataProfilePayload
        {
            DataSetName = "Sales",
            RowCount = 1000
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetProfile("Sales", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/datasets/Sales/profile");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.DataSetName.ShouldBe("Sales");
        result.Value.RowCount.ShouldBe(1000);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetProfileReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network error"));
        var sut = CreateClient(handler);

        var result = await sut.GetProfile("Sales", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task RunProfileSendsCorrectRequest()
    {
        var expected = new DataProfilePayload
        {
            DataSetName = "Orders",
            RowCount = 500
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.RunProfile("Orders", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/datasets/Orders/profile");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.DataSetName.ShouldBe("Orders");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task RunProfileReturnsFailureOnNonSuccess()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest));
        var sut = CreateClient(handler);

        var result = await sut.RunProfile("Bad", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task RunProfileReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network error"));
        var sut = CreateClient(handler);

        var result = await sut.RunProfile("Err", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }
}
