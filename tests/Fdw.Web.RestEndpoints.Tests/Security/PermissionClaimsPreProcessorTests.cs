using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Web.RestEndpoints.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;

namespace Fdw.Web.RestEndpoints.Tests.Security;

/// <summary>
/// Unit tests for <see cref="PermissionClaimsPreProcessor"/>.
/// Tests the grant/deny/skip logic against baked perm claims in the JWT.
/// </summary>
public sealed class PermissionClaimsPreProcessorTests
{
    private static IPreProcessorContext BuildContext(
        string? policyName,
        IEnumerable<Claim>? userClaims = null,
        bool isAuthenticated = true)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = services };

        // Set up an authenticated user with the specified claims
        var identity = new ClaimsIdentity(
            userClaims ?? [],
            isAuthenticated ? "Bearer" : null);
        httpContext.User = new ClaimsPrincipal(identity);

        // Set up endpoint metadata with an IAuthorizeData policy name
        IAuthorizeData? authorizeData = policyName is not null
            ? new AuthorizeAttribute { Policy = policyName }
            : null;

        var metadata = authorizeData is not null
            ? new EndpointMetadataCollection(authorizeData)
            : EndpointMetadataCollection.Empty;

        var endpoint = new Endpoint(null, metadata, "test");
        httpContext.SetEndpoint(endpoint);

        // Use a real BodyStream for writing
        httpContext.Response.Body = new System.IO.MemoryStream();

        var mock = new Mock<IPreProcessorContext>();
        mock.Setup(c => c.HttpContext).Returns(httpContext);

        return mock.Object;
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task PreProcessAsync_PermClaimGrantsAccess_DoesNotSet403()
    {
        // Arrange: token has perm claim matching the endpoint's policy
        var context = BuildContext(
            policyName: "users:read",
            userClaims:
            [
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, "42"),
                new Claim("perm", "users:read")
            ]);
        var sut = new PermissionClaimsPreProcessor();

        // Act
        await sut.PreProcessAsync(context, CancellationToken.None);

        // Assert — 200 is default, not changed to 403
        context.HttpContext.Response.StatusCode.ShouldNotBe(403);
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task PreProcessAsync_WildcardPermGrantsAccess_DoesNotSet403()
    {
        // Arrange: token has resource:* wildcard
        var context = BuildContext(
            policyName: "users:read",
            userClaims:
            [
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, "42"),
                new Claim("perm", "users:*")
            ]);
        var sut = new PermissionClaimsPreProcessor();

        // Act
        await sut.PreProcessAsync(context, CancellationToken.None);

        // Assert
        context.HttpContext.Response.StatusCode.ShouldNotBe(403);
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task PreProcessAsync_GlobalWildcard_DoesNotGrantAccess_Returns403()
    {
        // Arrange: token has the *:* super-grant
        var context = BuildContext(
            policyName: "connections:delete",
            userClaims:
            [
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, "1"),
                new Claim("perm", "*:*")
            ]);
        var sut = new PermissionClaimsPreProcessor();

        // Act
        await sut.PreProcessAsync(context, CancellationToken.None);

        // Assert — the *:* super-grant was deliberately removed; only resource:action and
        // resource:* are accepted. OpenIddict tokens carry explicit perms; no wildcard bypass.
        context.HttpContext.Response.StatusCode.ShouldBe(403);
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task PreProcessAsync_MissingPermClaim_Returns403()
    {
        // Arrange: token has perm claims, but not the required one
        var context = BuildContext(
            policyName: "connections:delete",
            userClaims:
            [
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, "42"),
                new Claim("perm", "users:read"),    // has a different perm
                new Claim("perm", "datastores:read")
            ]);
        var sut = new PermissionClaimsPreProcessor();

        // Act
        await sut.PreProcessAsync(context, CancellationToken.None);

        // Assert — 403 because required permission not in baked perm claims
        context.HttpContext.Response.StatusCode.ShouldBe(403);
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task PreProcessAsync_NoPermClaims_SkipsCheck()
    {
        // Arrange: token has no perm claims at all (legacy token — pre perm baking)
        var context = BuildContext(
            policyName: "users:read",
            userClaims:
            [
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, "42")
                // no perm claims
            ]);
        var sut = new PermissionClaimsPreProcessor();

        // Act
        await sut.PreProcessAsync(context, CancellationToken.None);

        // Assert — skips the check, defers to FdwAuthorizationPolicyProvider
        context.HttpContext.Response.StatusCode.ShouldNotBe(403);
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task PreProcessAsync_EpPolicyName_SkipsCheck()
    {
        // Arrange: epPolicy: prefix is FastEndpoints internal — not a resource:action permission
        var context = BuildContext(
            policyName: "epPolicy:Reference.Api.Endpoints.ListUsersEndpoint",
            userClaims:
            [
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, "42"),
                new Claim("perm", "users:read")
            ]);
        var sut = new PermissionClaimsPreProcessor();

        // Act
        await sut.PreProcessAsync(context, CancellationToken.None);

        // Assert — epPolicy: policies are skipped
        context.HttpContext.Response.StatusCode.ShouldNotBe(403);
    }

    [Fact]
    [Trait("Category", "Authorization")]
    public async Task PreProcessAsync_UnauthenticatedRequest_SkipsCheck()
    {
        // Arrange: unauthenticated user — JWT middleware handles 401 separately
        var context = BuildContext(
            policyName: "users:read",
            userClaims: [],
            isAuthenticated: false);
        var sut = new PermissionClaimsPreProcessor();

        // Act — should not throw, not set 403
        await sut.PreProcessAsync(context, CancellationToken.None);

        // Assert — unauthenticated skipped (handled by bearer middleware)
        context.HttpContext.Response.StatusCode.ShouldNotBe(403);
    }
}
