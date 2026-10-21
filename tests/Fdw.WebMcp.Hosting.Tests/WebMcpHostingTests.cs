using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.WebMcp.Hosting.Tests;

/// <summary>
/// The WebMCP three-phase registrant: it maps the script only when Initialize runs, and refuses a host
/// that cannot say what the script should carry.
/// </summary>
public class WebMcpHostingTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ADeferredInitializeClaimsThePhaseWithoutMappingTheScript()
    {
        using var app = Composed(withConfiguration: true);

        WebMcpHosting.Initialize(app, defer: true).IsSuccess.ShouldBeTrue();

        Routes(app).ShouldNotContain(WebMcpHosting.ScriptPath);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void InitializeMapsTheScript()
    {
        using var app = Composed(withConfiguration: true);

        var result = WebMcpHosting.Initialize(app);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Routes(app).ShouldContain(WebMcpHosting.ScriptPath);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AHostWithoutTheWebMcpConfigurationDomainIsRefused()
    {
        using var app = Composed(withConfiguration: false);

        var result = WebMcpHosting.Initialize(app);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(nameof(IWebMcpConfigurationProvider));
        Routes(app).ShouldNotContain(WebMcpHosting.ScriptPath);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TheScriptIsRevalidatedOnEveryRequestByItsOwnTag()
    {
        using var app = Composed(withConfiguration: true);
        WebMcpHosting.Initialize(app).IsSuccess.ShouldBeTrue();

        var first = await Requested(app, ifNoneMatch: null);

        first.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        first.Response.Headers.CacheControl.ToString().ShouldBe("no-cache");
        var tag = first.Response.Headers.ETag.ToString();
        tag.ShouldStartWith("\"");
        first.Response.Body.Length.ShouldBeGreaterThan(0);

        // A cache that compressed the script weakens the tag; the copy it holds is still this one.
        var again = await Requested(app, ifNoneMatch: "W/" + tag);

        again.Response.StatusCode.ShouldBe(StatusCodes.Status304NotModified);
        again.Response.Headers.ETag.ToString().ShouldBe(tag);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ACopyOfAnotherCatalogueIsAnsweredInFull()
    {
        using var app = Composed(withConfiguration: true);
        WebMcpHosting.Initialize(app).IsSuccess.ShouldBeTrue();

        var answered = await Requested(app, ifNoneMatch: "\"an-older-catalogue\"");

        answered.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        answered.Response.Body.Length.ShouldBeGreaterThan(0);
    }

    /// <summary>One GET of the script, through the endpoint the app mapped for it.</summary>
    private static async Task<HttpContext> Requested(WebApplication app, string? ifNoneMatch)
    {
        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(static source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(static e => string.Equals("/" + e.RoutePattern.RawText!.TrimStart('/'), WebMcpHosting.ScriptPath, StringComparison.Ordinal));
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = WebMcpHosting.ScriptPath;
        if (ifNoneMatch is not null)
        {
            context.Request.Headers.IfNoneMatch = ifNoneMatch;
        }

        context.Response.Body = new MemoryStream();
        await endpoint.RequestDelegate!(context);
        return context;
    }

    /// <summary>The route patterns the app has mapped.</summary>
    private static string[] Routes(WebApplication app)
        => ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(static source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(static endpoint => "/" + endpoint.RoutePattern.RawText!.TrimStart('/'))
            .ToArray();

    /// <summary>A web host with the registrant registered, and a WebMcp row when asked for one.</summary>
    private static WebApplication Composed(bool withConfiguration)
    {
        var builder = WebApplication.CreateBuilder();
        if (withConfiguration)
        {
            var configurations = new Mock<IWebMcpConfigurationProvider>();
            configurations.Setup(p => p.Get(WebMcpHosting.ConfigurationName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IWebMcpImplementationConfiguration>.Success(new WebMcpRow()));
            builder.Services.AddSingleton(configurations.Object);
        }

        builder.Services.AddSingleton(new Mock<ISecretManagerProvider>().Object);
        WebMcpHosting.Register(builder).IsSuccess.ShouldBeTrue();
        return builder.Build();
    }
}
