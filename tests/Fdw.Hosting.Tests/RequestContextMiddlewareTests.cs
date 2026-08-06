using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Fdw.Hosting.Middleware;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.AspNetCore.Http;

namespace Fdw.Hosting.Tests;

/// <summary>
/// Tests for <see cref="RequestContextMiddleware"/>'s wiring of
/// <see cref="IAuthenticationContextAccessor.Current"/> — the per-request establishment of the
/// ambient <see cref="IAuthenticationContext"/> that <c>MsSqlConnection.SetUserSessionContext</c>
/// reads for RLS SESSION_CONTEXT.
/// </summary>
public class RequestContextMiddlewareTests
{
    private static RequestContextMiddleware CreateMiddleware(IAuthenticationContextAccessor accessor)
        => new(context => Task.CompletedTask, logger: null, authContextAccessor: accessor);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task InvokeSetsCurrentFromAuthenticatedClaimsPrincipal()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimDefinitions.sub.Name, userId.ToString()),
        ], authenticationType: "TestAuthType");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        var accessor = new AuthenticationContextAccessor();
        var middleware = CreateMiddleware(accessor);

        // Act
        await middleware.Invoke(context);

        // Assert
        accessor.Current.ShouldNotBeNull();
        accessor.Current.ShouldBeOfType<ClaimsPrincipalAuthenticationContext>();
        accessor.Current!.UserId.ShouldBe(userId.ToString());
        accessor.Current.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task InvokeLeavesCurrentNullForAnonymousRequest()
    {
        // Arrange: default DefaultHttpContext.User has an unauthenticated identity.
        var context = new DefaultHttpContext();

        var accessor = new AuthenticationContextAccessor();
        var middleware = CreateMiddleware(accessor);

        // Act
        await middleware.Invoke(context);

        // Assert: fail closed — no established identity means no SESSION_CONTEXT will be set,
        // never a fallback to any elevated visibility.
        accessor.Current.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task InvokeOverwritesAPreviouslySetCurrentWhenNewRequestIsAnonymous()
    {
        // Arrange
        // Why: proves the middleware does not merely "set when authenticated" — it must also
        // explicitly clear a stale value so one request's identity never leaks into the next
        // request handled on a flow that happens to reuse the same accessor instance.
        var accessor = new AuthenticationContextAccessor
        {
            Current = new WorkAuthenticationContext(Guid.NewGuid()),
        };
        var middleware = CreateMiddleware(accessor);
        var anonymousContext = new DefaultHttpContext();

        // Act
        await middleware.Invoke(anonymousContext);

        // Assert
        accessor.Current.ShouldBeNull();
    }
}
