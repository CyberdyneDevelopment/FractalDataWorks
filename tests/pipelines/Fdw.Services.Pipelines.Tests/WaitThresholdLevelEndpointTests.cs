using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Etl.Thresholds;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// FDW-3161: the routes of the level of thresholds on waiting runs hand the owner the route names (the server, or the pipeline or schedule in the path) and the body to the
/// store, answer the level it returns, and answer a failure with the status its result code names: a refusal 400, an owner that does not exist 404.
/// </summary>
[Trait("Category", "Etl")]
public class WaitThresholdLevelEndpointTests
{
    private sealed class GetServer(IWaitThresholdStore store) : GetWaitThresholdLevelEndpointBase(store)
    {
        protected override string OwnerKind => WaitThresholdOwners.Server;

        protected override string RoutePath => "/etl/wait-thresholds/server";
    }

    private sealed class GetPipeline(IWaitThresholdStore store) : GetWaitThresholdLevelEndpointBase(store)
    {
        protected override string OwnerKind => WaitThresholdOwners.Pipeline;

        protected override string RoutePath => "/etl/wait-thresholds/pipelines/{name}";
    }

    private sealed class PutSchedule(IWaitThresholdStore store) : PutWaitThresholdLevelEndpointBase(store)
    {
        protected override string OwnerKind => WaitThresholdOwners.Schedule;

        protected override string RoutePath => "/etl/wait-thresholds/schedules/{name}";
    }

    private static Action<DefaultHttpContext> Named(string name) => ctx =>
    {
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "caller")], "test"));
        ctx.Request.RouteValues["name"] = name;
    };

    private static readonly WaitThresholdLevelPayload Own = new()
    {
        Mode = "Own",
        Thresholds = [new WaitThresholdPayload
        {
            Name = "Queue full", Comparison = "GreaterThanOrEqual", Number = 100, Severity = "Error", NotificationRules = ["Ops email"], StopsNewRuns = true, IsEnabled = true,
        }],
    };

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TheServerRouteReadsTheServerLevelWithNoOwnerName()
    {
        var store = new Mock<IWaitThresholdStore>();
        store.Setup(s => s.Read("Server", null, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<WaitThresholdLevelPayload>.Success(Own));
        var endpoint = Factory.Create<GetServer>(store.Object);

        await endpoint.HandleAsync(EmptyRequest.Instance, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldBeSameAs(Own);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ThePipelineRouteReadsTheLevelOfThePipelineTheRouteNames()
    {
        var store = new Mock<IWaitThresholdStore>();
        store.Setup(s => s.Read("Pipeline", "AfcPlayersLoad", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<WaitThresholdLevelPayload>.Success(new WaitThresholdLevelPayload { Mode = "Inherit" }));
        var endpoint = Factory.Create<GetPipeline>(Named("AfcPlayersLoad"), store.Object);

        await endpoint.HandleAsync(EmptyRequest.Instance, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Mode.ShouldBe("Inherit");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOwnerThatDoesNotExistIsA404()
    {
        var store = new Mock<IWaitThresholdStore>();
        store.Setup(s => s.Read("Pipeline", "Ghost", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<WaitThresholdLevelPayload>.Failure(WaitThresholdLog.NoSuchOwner(NullLogger.Instance, "Pipeline", "Ghost")));
        var endpoint = Factory.Create<GetPipeline>(Named("Ghost"), store.Object);

        await endpoint.HandleAsync(EmptyRequest.Instance, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ThePutRouteWritesTheBodyForTheScheduleTheRouteNamesAndAnswersTheStoredLevel()
    {
        var store = new Mock<IWaitThresholdStore>();
        store.Setup(s => s.Write("Schedule", "Nightly", Own, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<WaitThresholdLevelPayload>.Success(Own));
        var endpoint = Factory.Create<PutSchedule>(Named("Nightly"), store.Object);

        await endpoint.HandleAsync(Own, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldBeSameAs(Own);
        store.Verify(s => s.Write("Schedule", "Nightly", Own, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ALevelTheStoreRefusesIsA400()
    {
        var store = new Mock<IWaitThresholdStore>();
        store.Setup(s => s.Write("Schedule", "Nightly", Own, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<WaitThresholdLevelPayload>.Failure(WaitThresholdLog.Refused(NullLogger.Instance, "thresholds[0].number", "it is negative")));
        var endpoint = Factory.Create<PutSchedule>(Named("Nightly"), store.Object);

        await endpoint.HandleAsync(Own, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(400);
    }
}
