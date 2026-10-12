using System.Linq;
using System.Threading;
using Fdw.Results;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.AspNetCore.Builder;
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
