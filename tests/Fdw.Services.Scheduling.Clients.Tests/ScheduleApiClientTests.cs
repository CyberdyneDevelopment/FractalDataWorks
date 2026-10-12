using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Clients.Tests;

public sealed class ScheduleHttpClientTests
{
    private static readonly Uri BaseUri = new("https://test.example.com/");

    private static ScheduleHttpClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = BaseUri };
        return new ScheduleHttpClient(httpClient, Mock.Of<ILogger<ScheduleHttpClient>>());
    }

    // --- List ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task ListSendsGetRequestToCorrectPath()
    {
        var expected = new List<ScheduleInfoDto>
        {
            new() { Name = "daily-etl", PipelineName = "etl-pipeline" }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create<IReadOnlyList<ScheduleInfoDto>>(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.List(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/schedules");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
        result.Value[0].Name.ShouldBe("daily-etl");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task ListReturnsFailureOnError()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.List(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- Get ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task GetSendsGetRequestWithNameInPath()
    {
        var expected = new ScheduleInfoDto { Name = "daily-etl", PipelineName = "etl-pipeline", IsEnabled = true };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.Get("daily-etl", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/schedules/daily-etl");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("daily-etl");
        result.Value.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task GetReturnsFailureOnError()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.Get("daily-etl", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- CreateSchedule ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task CreateScheduleSendsPostRequestToCorrectPath()
    {
        var response = new CreateScheduleClientResponse { Id = Guid.CreateVersion7(), Name = "daily-etl" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(response)
        });
        var sut = CreateClient(handler);
        var request = new CreateScheduleClientRequest { Name = "daily-etl", PipelineName = "etl-pipeline", CronExpression = "0 0 * * *" };

        var result = await sut.CreateSchedule(request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/schedules");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task CreateScheduleReturnsFailureOnNonSuccessStatus()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest));
        var sut = CreateClient(handler);
        var request = new CreateScheduleClientRequest { Name = "daily-etl", PipelineName = "etl-pipeline", CronExpression = "0 0 * * *" };

        var result = await sut.CreateSchedule(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task CreateScheduleReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);
        var request = new CreateScheduleClientRequest { Name = "daily-etl", PipelineName = "etl-pipeline", CronExpression = "0 0 * * *" };

        var result = await sut.CreateSchedule(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- UpdateSchedule ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task UpdateScheduleSendsPatchRequestWithNameInPath()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateClient(handler);
        var request = new UpdateScheduleClientRequest { PipelineName = "etl-pipeline", CronExpression = "0 1 * * *" };

        var result = await sut.UpdateSchedule("daily-etl", request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/schedules/daily-etl");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Patch);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task UpdateScheduleReturnsFailureOnNonSuccessStatus()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = CreateClient(handler);
        var request = new UpdateScheduleClientRequest { PipelineName = "etl-pipeline" };

        var result = await sut.UpdateSchedule("daily-etl", request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task UpdateScheduleReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);
        var request = new UpdateScheduleClientRequest { PipelineName = "etl-pipeline" };

        var result = await sut.UpdateSchedule("daily-etl", request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- DeleteSchedule ---

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task DeleteScheduleSendsDeleteRequestWithNameInPath()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateClient(handler);

        var result = await sut.DeleteSchedule("daily-etl", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/schedules/daily-etl");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Delete);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task DeleteScheduleReturnsFailureOnNonSuccessStatus()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = CreateClient(handler);

        var result = await sut.DeleteSchedule("daily-etl", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Scheduling")]
    public async Task DeleteScheduleReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var sut = CreateClient(handler);

        var result = await sut.DeleteSchedule("daily-etl", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }
}
