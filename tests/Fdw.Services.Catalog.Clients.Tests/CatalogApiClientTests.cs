using System.Net;
using System.Net.Http.Json;
using Fdw.Services.Catalog.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Catalog.Clients.Tests;

public sealed class CatalogApiClientTests
{
    private static CatalogApiClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.example.com/") };
        return new CatalogApiClient(httpClient, Mock.Of<ILogger<CatalogApiClient>>());
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task SearchWithoutEntityTypeSendsCorrectRequest()
    {
        var expected = new List<CatalogEntityPayload>
        {
            new() { Id = Guid.NewGuid(), Name = "Entity1", EntityType = "DataSet" }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.Search("test query", ct: TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/catalog/search?query=test%20query");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task SearchWithEntityTypeSendsCorrectRequest()
    {
        var expected = new List<CatalogEntityPayload>
        {
            new() { Id = Guid.NewGuid(), Name = "Entity1", EntityType = "DataSet" }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.Search("test", "DataSet", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/catalog/search?query=test&entityType=DataSet");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task SearchWithNullEntityTypeDoesNotAppendParameter()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<CatalogEntityPayload>())
        });
        var sut = CreateClient(handler);

        await sut.Search("q", null, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldNotContain("entityType");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task SearchWithEmptyEntityTypeDoesNotAppendParameter()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<CatalogEntityPayload>())
        });
        var sut = CreateClient(handler);

        await sut.Search("q", "", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldNotContain("entityType");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetGlossarySendsCorrectRequest()
    {
        var expected = new List<GlossaryTermPayload>
        {
            new() { Id = Guid.NewGuid(), Term = "ETL", Definition = "Extract Transform Load" }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetGlossary(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/catalog/glossary");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetDataSetEntrySendsCorrectRequest()
    {
        var expected = new DataSetCatalogPayload
        {
            Id = Guid.NewGuid(), Name = "MyDataSet", Description = "Test description"
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetDataSetEntry("MyDataSet", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/catalog/datasets/MyDataSet");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("MyDataSet");
    }
}
