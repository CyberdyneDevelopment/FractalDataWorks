using System.Net;
using System.Net.Http.Json;
using Fdw.Services.Quality.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality.Clients.Tests;

public sealed class QualityApiClientTests
{
    private static QualityApiClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.example.com/") };
        return new QualityApiClient(httpClient, Mock.Of<ILogger<QualityApiClient>>());
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetDashboardSendsCorrectRequest()
    {
        var expected = new QualityDashboardPayload { TotalRules = 10, PassingRules = 8, FailingRules = 2 };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetDashboard(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/quality/dashboard");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.TotalRules.ShouldBe(10);
        result.Value!.PassingRules.ShouldBe(8);
        result.Value!.FailingRules.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetRulesSendsCorrectRequest()
    {
        var expected = new List<QualityRuleSummaryPayload>
        {
            new() { Id = Guid.NewGuid(), Name = "Rule1", Description = "Desc1", IsEnabled = true }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetRules(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/quality/rules");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task GetRuleSendsCorrectRequest()
    {
        var id = Guid.NewGuid();
        var expected = new QualityRuleDetailPayload
        {
            Id = id, Name = "Rule1", Description = "Desc1",
            RuleType = "Completeness", Expression = "count > 0", IsEnabled = true
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetRule(id, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe($"/quality/rules/{id}");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Id.ShouldBe(id);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task CreateRuleSendsCorrectRequest()
    {
        var request = new CreateQualityRulePayload
        {
            Name = "NewRule", Description = "New rule desc",
            RuleType = "Completeness", Expression = "count > 0"
        };
        var expected = new QualityRuleDetailPayload
        {
            Id = Guid.NewGuid(), Name = "NewRule", Description = "New rule desc",
            RuleType = "Completeness", Expression = "count > 0", IsEnabled = true
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.CreateRule(request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/quality/rules");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("NewRule");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task UpdateRuleSendsCorrectRequest()
    {
        var id = Guid.NewGuid();
        var request = new UpdateQualityRulePayload
        {
            Name = "UpdatedRule", Description = "Updated desc",
            Expression = "count > 10", IsEnabled = false
        };
        var expected = new QualityRuleDetailPayload
        {
            Id = id, Name = "UpdatedRule", Description = "Updated desc",
            RuleType = "Completeness", Expression = "count > 10", IsEnabled = false
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.UpdateRule(id, request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe($"/quality/rules/{id}");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Patch);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("UpdatedRule");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task DeleteRuleSendsCorrectRequest()
    {
        var id = Guid.NewGuid();
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateClient(handler);

        var result = await sut.DeleteRule(id, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe($"/quality/rules/{id}");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Delete);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task ExecuteCheckSendsCorrectRequest()
    {
        var id = Guid.NewGuid();
        var expected = new QualityCheckResultPayload
        {
            RuleId = id, RuleName = "Rule1", Passed = true, Message = "All checks passed"
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.ExecuteCheck(id, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe($"/quality/rules/{id}/execute");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Passed.ShouldBeTrue();
    }
}
