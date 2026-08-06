using System.Net;
using System.Net.Http.Json;
using Fdw.Web.Search.Clients.ApiClients;
using Fdw.Web.Search.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.Search.Clients.Tests;

public sealed class SearchApiClientTests
{
    private static SearchApiClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.example.com/") };
        return new SearchApiClient(httpClient, Mock.Of<ILogger<SearchApiClient>>());
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task SearchSendsCorrectRequest()
    {
        var expected = new SearchResponse
        {
            Query = "test",
            TotalCount = 1,
            Results = [new SearchResultPayload { Name = "result1", Type = "DataStore", Url = "/datastores/1" }]
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);
        var request = new SearchRequest { Query = "test", Limit = 20 };

        var result = await sut.Search(request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/search?query=test&limit=20");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Query.ShouldBe("test");
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Results.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task SearchEncodesSpecialCharacters()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new SearchResponse { Query = "hello world" })
        });
        var sut = CreateClient(handler);
        var request = new SearchRequest { Query = "hello world", Limit = 10 };

        await sut.Search(request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/search?query=hello%20world&limit=10");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task SearchReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network error"));
        var sut = CreateClient(handler);
        var request = new SearchRequest { Query = "fail" };

        var result = await sut.Search(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task SearchWithCustomLimitSendsCorrectLimit()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new SearchResponse { Query = "q" })
        });
        var sut = CreateClient(handler);
        var request = new SearchRequest { Query = "q", Limit = 50 };

        await sut.Search(request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/search?query=q&limit=50");
    }
}
