using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Tests for <see cref="FrameworkPermissionHandler"/> — the ASP.NET Core authorization bridge that
/// gates every secured endpoint. A wrongly-succeeding handler opens every endpoint, so these tests
/// pin: the unauthenticated short-circuit, the inner Success(true) -> Succeed path, and the
/// fail-closed behavior for both inner Success(false) and inner Failure (handler must NOT call
/// context.Succeed in either case).
/// </summary>
public sealed class FrameworkPermissionHandlerTests
{
    private readonly Mock<IFrameworkAuthorizationService> _authorizationServiceMock = new();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void Constructor_Throws_WhenAuthorizationServiceNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            new FrameworkPermissionHandler(null!, NullLogger<FrameworkPermissionHandler>.Instance));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void Constructor_Throws_WhenLoggerNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            new FrameworkPermissionHandler(_authorizationServiceMock.Object, null!));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task HandleRequirementAsync_UnauthenticatedUser_DoesNotSucceed_AndSkipsAuthorizeCall()
    {
        // Why: ClaimsIdentity() with no authenticationType yields IsAuthenticated == false.
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var context = BuildContext(principal, "connections", "read");
        var sut = CreateHandler();

        await sut.HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
        _authorizationServiceMock.Verify(
            s => s.Authorize(It.IsAny<IAuthenticationContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task HandleRequirementAsync_NoIdentityAtAll_DoesNotSucceed()
    {
        // Why: a bare ClaimsPrincipal() has no primary identity — Identity is null, so the
        // null-conditional short-circuit (`context.User.Identity?.IsAuthenticated != true`) must hold.
        var principal = new ClaimsPrincipal();
        var context = BuildContext(principal, "connections", "read");
        var sut = CreateHandler();

        await sut.HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
        _authorizationServiceMock.Verify(
            s => s.Authorize(It.IsAny<IAuthenticationContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task HandleRequirementAsync_InnerServiceGrants_Succeeds()
    {
        var principal = CreateAuthenticatedPrincipal();
        var context = BuildContext(principal, "connections", "read");
        _authorizationServiceMock
            .Setup(s => s.Authorize(It.IsAny<IAuthenticationContext>(), "connections", "read", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Success(true));
        var sut = CreateHandler();

        await sut.HandleAsync(context);

        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task HandleRequirementAsync_InnerServiceDeniesSuccessfully_DoesNotSucceed()
    {
        // Why: a successful result carrying Value == false (a clean "not authorized" decision)
        // must NOT call context.Succeed — fail-closed.
        var principal = CreateAuthenticatedPrincipal();
        var context = BuildContext(principal, "connections", "write");
        _authorizationServiceMock
            .Setup(s => s.Authorize(It.IsAny<IAuthenticationContext>(), "connections", "write", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Success(false));
        var sut = CreateHandler();

        await sut.HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task HandleRequirementAsync_InnerServiceFails_DoesNotSucceed()
    {
        // Why: when the inner authorization service itself fails (e.g. a provider query error),
        // the handler must fail-closed rather than treat the failure as an implicit grant.
        var principal = CreateAuthenticatedPrincipal();
        var context = BuildContext(principal, "connections", "delete");
        _authorizationServiceMock
            .Setup(s => s.Authorize(It.IsAny<IAuthenticationContext>(), "connections", "delete", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Failure(Fdw.Services.Authorization.Logging.AuthorizationLog.AuthorizationContextNull(NullLogger.Instance)));
        var sut = CreateHandler();

        await sut.HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
    }

    private FrameworkPermissionHandler CreateHandler() =>
        new(_authorizationServiceMock.Object, NullLogger<FrameworkPermissionHandler>.Instance);

    private static ClaimsPrincipal CreateAuthenticatedPrincipal() =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "user-1")],
            authenticationType: "TestAuth"));

    private static AuthorizationHandlerContext BuildContext(ClaimsPrincipal principal, string resource, string action) =>
        new(
            new List<IAuthorizationRequirement> { new FdwPermissionRequirement(resource, action) },
            principal,
            resource: null);
}
