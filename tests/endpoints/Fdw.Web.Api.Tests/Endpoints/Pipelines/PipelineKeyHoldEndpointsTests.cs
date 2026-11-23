using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Execution.Claims;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for the two routes of the hold on a key (FDW-6939): <c>POST /pipeline-keys/{Key}/hold</c> places it and <c>DELETE</c> releases it. The person is the signed-in caller and the tenant the caller's, never
/// from the body; a caller that is no person, or acts in no tenant, is refused before the service is asked; the service's own refusals (a key that is held, a key that is not held, no reason) are answered with
/// a client status. The permission is <c>pipelines:hold</c>, stated in <c>Configure</c>, so a deployment gives holding to other people than starting and cancelling.
/// </summary>
public sealed class PipelineKeyHoldEndpointsTests
{
    private static readonly Guid Tenant = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid Person = Guid.Parse("55555555-0000-0000-0000-000000000005");

    private readonly Mock<IPipelineKeyHolds> _holds = new(MockBehavior.Strict);
    private readonly Mock<IAuthenticationContextAccessor> _authentication = new();

    // The tenant is the one the caller's token states (ActiveTenantId), the way every host has it: the ETL host resolves no tenant per request, so there is no tenant context to stand in
    // for it here, and none is given to the endpoints (FDW-6977).
    private void Caller(string userId, bool authenticated = true, Guid? tenant = null)
    {
        var context = new Mock<IAuthenticationContext>();
        context.SetupGet(c => c.IsAuthenticated).Returns(authenticated);
        context.SetupGet(c => c.IsSystemContext).Returns(false);
        context.SetupGet(c => c.UserId).Returns(userId);
        context.SetupGet(c => c.ActiveTenantId).Returns(tenant);
        _authentication.SetupGet(a => a.Current).Returns(context.Object);
    }

    private sealed class PlaceProbe(IPipelineKeyHolds holds, IAuthenticationContextAccessor authentication)
        : PlacePipelineKeyHoldEndpointBase(NullLogger.Instance, holds, authentication);

    private sealed class ReleaseProbe(IPipelineKeyHolds holds, IAuthenticationContextAccessor authentication)
        : ReleasePipelineKeyHoldEndpointBase(NullLogger.Instance, holds, authentication);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task APersonInATenantPlacesAHoldWithTheirOwnIdAndTheTenantAndTheKeyAndReasonTheyState()
    {
        Caller(Person.ToString("D"), tenant: Tenant);
        var at = DateTimeOffset.Parse("2026-11-19T12:00:00+00:00");
        _holds.Setup(h => h.Place(Tenant, "chat-9", Person, "waiting for the answer", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<PipelineKeyHold>.Success(new PipelineKeyHold(Person, at, "waiting for the answer")));
        var endpoint = Factory.Create<PlaceProbe>(_holds.Object, _authentication.Object);

        await endpoint.HandleAsync(new PlacePipelineKeyHoldRequest { Key = "chat-9", Reason = "waiting for the answer" }, TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        _holds.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ACallerInThePlatformTenantHoldsAKeyOfThatTenantLikeAnyOther()
    {
        // FDW-6977: the seeded admin acts in the platform tenant (all F). It is a tenant like any other; the ETL host resolves no tenant per request, so the token's own claim is the only source.
        var platform = Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");
        Caller(Person.ToString("D"), tenant: platform);
        _holds.Setup(h => h.Place(platform, "chat-9", Person, "a reason", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<PipelineKeyHold>.Success(new PipelineKeyHold(Person, DateTimeOffset.Parse("2026-11-19T12:00:00+00:00"), "a reason")));
        var endpoint = Factory.Create<PlaceProbe>(_holds.Object, _authentication.Object);

        await endpoint.HandleAsync(new PlacePipelineKeyHoldRequest { Key = "chat-9", Reason = "a reason" }, TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        _holds.VerifyAll();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    [InlineData("not-a-person-id", true)]
    [InlineData("55555555-0000-0000-0000-000000000005", false)]
    public async Task ACallerThatIsNoPersonIsRefusedAsUnauthenticatedBeforeTheServiceIsAsked(string userId, bool authenticated)
    {
        Caller(userId, authenticated, tenant: Tenant);
        var endpoint = Factory.Create<PlaceProbe>(_holds.Object, _authentication.Object);

        await endpoint.HandleAsync(new PlacePipelineKeyHoldRequest { Key = "chat-9", Reason = "a reason" }, TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(401);
        _holds.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ACallerWhoseTokenStatesNoTenantIsRefusedBeforeTheServiceIsAsked()
    {
        Caller(Person.ToString("D"));
        var endpoint = Factory.Create<PlaceProbe>(_holds.Object, _authentication.Object);

        await endpoint.HandleAsync(new PlacePipelineKeyHoldRequest { Key = "chat-9", Reason = "a reason" }, TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(400);
        _holds.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task APlaceOnAKeyThatIsHeldIsAnsweredWithAConflict()
    {
        Caller(Person.ToString("D"), tenant: Tenant);
        _holds.Setup(h => h.Place(Tenant, "chat-9", Person, "a reason", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<PipelineKeyHold>.Failure(PipelineRunClaimLog.KeyAlreadyHeld(NullLogger.Instance, "chat-9", Guid.NewGuid(), "2026-11-19T12:00:00Z", "waiting")));
        var endpoint = Factory.Create<PlaceProbe>(_holds.Object, _authentication.Object);

        await endpoint.HandleAsync(new PlacePipelineKeyHoldRequest { Key = "chat-9", Reason = "a reason" }, TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AnyCallerWithThePermissionInTheTenantReleasesTheHoldAndTheRunsItPromotedAreAnswered()
    {
        Caller(Person.ToString("D"), tenant: Tenant);
        _holds.Setup(h => h.Release(Tenant, "chat-9", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<PipelineKeyHoldReleased>.Success(new PipelineKeyHoldReleased(Guid.NewGuid(), [])));
        var endpoint = Factory.Create<ReleaseProbe>(_holds.Object, _authentication.Object);

        await endpoint.HandleAsync(new ReleasePipelineKeyHoldRequest { Key = "chat-9" }, TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        _holds.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AReleaseOfAKeyThatIsNotHeldIsAnsweredAsNotFound()
    {
        Caller(Person.ToString("D"), tenant: Tenant);
        _holds.Setup(h => h.Release(Tenant, "chat-9", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<PipelineKeyHoldReleased>.Failure(PipelineRunClaimLog.KeyNotHeld(NullLogger.Instance, "chat-9")));
        var endpoint = Factory.Create<ReleaseProbe>(_holds.Object, _authentication.Object);

        await endpoint.HandleAsync(new ReleasePipelineKeyHoldRequest { Key = "chat-9" }, TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheTwoRoutesAreGatedByThePermissionOfTheirOwn()
    {
        var place = Factory.Create<PlaceProbe>(_holds.Object, _authentication.Object);
        var release = Factory.Create<ReleaseProbe>(_holds.Object, _authentication.Object);

        place.Definition.PreBuiltUserPolicies.ShouldNotBeNull().ShouldContain("pipelines:hold");
        release.Definition.PreBuiltUserPolicies.ShouldNotBeNull().ShouldContain("pipelines:hold");
        place.Definition.Routes.ShouldContain("/pipeline-keys/{Key}/hold");
        release.Definition.Routes.ShouldContain("/pipeline-keys/{Key}/hold");
        place.Definition.Verbs.ShouldContain("POST");
        release.Definition.Verbs.ShouldContain("DELETE");
    }
}
