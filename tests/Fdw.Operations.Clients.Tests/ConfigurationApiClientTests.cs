using System.Net;
using System.Net.Http.Json;
using Fdw.Operations.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Clients.Tests;

public sealed class ConfigurationApiClientTests
{
    private static ConfigurationApiClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.example.com/") };
        return new ConfigurationApiClient(httpClient, Mock.Of<ILogger<ConfigurationApiClient>>());
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetTypesByCategorySendsCorrectRequest()
    {
        var expected = new List<ConfigurationTypeSummary>
        {
            new() { TypeName = "MsSql", DisplayName = "SQL Server", Category = "Connection" }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetTypesByCategory("Connection", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/configuration/types?category=Connection");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetTypesByCategoryEscapesCategoryName()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<ConfigurationTypeSummary>())
        });
        var sut = CreateClient(handler);

        await sut.GetTypesByCategory("Data Store", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/configuration/types?category=Data%20Store");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetTypeDetailSendsCorrectRequest()
    {
        var expected = new ConfigurationTypeDetail
        {
            TypeName = "MsSql",
            DisplayName = "SQL Server",
            Category = "Connection"
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetTypeDetail("Connection", "MsSql", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/configuration/types/detail?category=Connection&type=MsSql");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.TypeName.ShouldBe("MsSql");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetRootTypesSendsCorrectRequest()
    {
        var expected = new List<ConfigurationTypeSummary>
        {
            new() { TypeName = "Connection", DisplayName = "Connection", Category = "Root" }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetRootTypes(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/configuration/types/roots");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetChildTypesSendsCorrectRequest()
    {
        var expected = new List<ConfigurationTypeSummary>
        {
            new() { TypeName = "MsSql", DisplayName = "SQL Server", Category = "Connection" }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetChildTypes("Connection", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/configuration/types/children?parent=Connection");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetInstancesWithNullCategoryUsesBasePath()
    {
        var expected = new List<ConfigurationInstanceSummaryPayload>
        {
            new() { Id = Guid.NewGuid(), Name = "Instance1", ServiceType = "MsSql", Category = "Connection" }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetInstances(null, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/configuration/instances");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetInstancesWithEmptyCategoryUsesBasePath()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<ConfigurationInstanceSummaryPayload>())
        });
        var sut = CreateClient(handler);

        var result = await sut.GetInstances("", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/configuration/instances");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetInstancesWithCategoryAppendsCategoryParameter()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<ConfigurationInstanceSummaryPayload>())
        });
        var sut = CreateClient(handler);

        var result = await sut.GetInstances("Connection", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/configuration/instances?category=Connection");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetInstancesWithCategoryEscapesValue()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<ConfigurationInstanceSummaryPayload>())
        });
        var sut = CreateClient(handler);

        await sut.GetInstances("Data Store", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/configuration/instances?category=Data%20Store");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetInstanceSendsCorrectRequest()
    {
        var expected = new ConfigurationInstanceDetailPayload
        {
            Id = Guid.NewGuid(),
            Name = "MyConn",
            ServiceType = "MsSql",
            Category = "Connection"
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetInstance("Connection", "MyConn", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/configuration/instances/Connection/MyConn");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("MyConn");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task CreateInstanceSendsCorrectRequest()
    {
        var request = new CreateConfigurationInstanceRequest
        {
            ServiceType = "MsSql",
            Name = "NewConn"
        };
        var expected = new ConfigurationInstanceDetailPayload
        {
            Id = Guid.NewGuid(),
            Name = "NewConn",
            ServiceType = "MsSql",
            Category = "Connection"
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.CreateInstance("Connection", request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/configuration/instances/Connection");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("NewConn");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task UpdateInstanceSendsCorrectRequest()
    {
        var request = new UpdateConfigurationInstanceRequest
        {
            Values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["Server"] = "newserver" }
        };
        var expected = new ConfigurationInstanceDetailPayload
        {
            Id = Guid.NewGuid(),
            Name = "MyConn",
            ServiceType = "MsSql",
            Category = "Connection"
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.UpdateInstance("Connection", "MyConn", request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/configuration/instances/Connection/MyConn");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Put);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DeleteInstanceSendsCorrectRequest()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateClient(handler);

        var result = await sut.DeleteInstance("Connection", "MyConn", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/configuration/instances/Connection/MyConn");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Delete);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DeleteInstanceReturnsFailureOnFailure()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = CreateClient(handler);

        var result = await sut.DeleteInstance("Connection", "Missing", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }
}
