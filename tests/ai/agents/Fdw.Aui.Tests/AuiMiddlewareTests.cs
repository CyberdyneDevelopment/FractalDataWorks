using System.Security.Claims;
using Fdw.Aui.Models;
using Fdw.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Aui.Tests;

public class AuiMiddlewareTests
{
    private const string AuiRoute = "/orders.aui";

    private static (AuiMiddleware Middleware, AuiService Service, Mock<IAuiProvider> Provider, Mock<RequestDelegate> Next) Subject()
    {
        var provider = new Mock<IAuiProvider>();
        provider
            .Setup(p => p.GetAuiManifest(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<AuiManifest>.Success(new AuiManifest { Description = "Orders" }));

        var next = new Mock<RequestDelegate>();
        var service = new AuiService([provider.Object], NullLogger<AuiService>.Instance);
        return (new AuiMiddleware(next.Object, NullLogger<AuiMiddleware>.Instance), service, provider, next);
    }

    private static DefaultHttpContext Request(ClaimsPrincipal? user)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider()
        };
        context.Request.Path = AuiRoute;
        context.Response.Body = new MemoryStream();
        if (user is not null)
        {
            context.User = user;
        }

        return context;
    }

    private static ClaimsPrincipal SignedIn(string sub) =>
        new(new ClaimsIdentity([new Claim("sub", sub)], "Bearer"));

    [Fact]
    public async Task Invoke_NobodySignedIn_Answers401AndBuildsNoManifest()
    {
        var (middleware, service, provider, _) = Subject();
        var context = Request(null);

        await middleware.Invoke(context, service);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        provider.Verify(p => p.GetAuiManifest(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Invoke_SubIsNotAGuid_Answers401AndBuildsNoManifest()
    {
        var (middleware, service, provider, _) = Subject();
        var context = Request(SignedIn("not-a-guid"));

        await middleware.Invoke(context, service);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        provider.Verify(p => p.GetAuiManifest(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Invoke_SignedIn_BuildsTheManifestForThatUser()
    {
        var (middleware, service, provider, _) = Subject();
        var userId = Guid.NewGuid();
        var context = Request(SignedIn(userId.ToString()));

        await middleware.Invoke(context, service);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        provider.Verify(p => p.GetAuiManifest(userId, "/orders", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Invoke_NotAnAuiRequest_PassesOnWithoutReadingTheUser()
    {
        var (middleware, service, provider, next) = Subject();
        var context = Request(null);
        context.Request.Path = "/orders";

        await middleware.Invoke(context, service);

        next.Verify(n => n(context), Times.Once);
        provider.Verify(p => p.GetAuiManifest(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
