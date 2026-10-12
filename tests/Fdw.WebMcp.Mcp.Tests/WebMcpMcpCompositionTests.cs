using System.Linq;
using System.Threading;
using Fdw.Results;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.WebMcp.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.WebMcp.Mcp.Tests;

/// <summary>
/// The MCP bridge's three-phase registrant: what it registers, and what stops it from being served.
/// </summary>
public class WebMcpMcpCompositionTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RegisterComposesTheMcpServerAndItsInvoker()
    {
        var builder = Host.CreateApplicationBuilder();

        WebMcpHosting.Register(builder).IsSuccess.ShouldBeTrue();
        var result = WebMcpMcpHosting.Register(builder);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        builder.Services.ShouldContain(descriptor => descriptor.ServiceType == typeof(WebMcpToolInvoker));
        builder.Services.ShouldContain(descriptor => descriptor.ServiceType == typeof(WebMcpMcpCatalogue));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ADeferredInitializeClaimsThePhaseWithoutMappingAnything()
    {
        using var app = Composed(new McpRow { SelfBaseAddress = "http://127.0.0.1:5510" });

        WebMcpMcpHosting.Initialize(app, defer: true).IsSuccess.ShouldBeTrue();

        RouteCount(app).ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AHostWithoutARouteTableIsRefused()
    {
        using var host = Host.CreateApplicationBuilder().Build();

        var result = WebMcpMcpHosting.Initialize(host);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("route table");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ARowNamingNoSelfAddressIsRefusedBeforeAnyToolCall()
    {
        // A bridge that cannot reach its own routes would accept a tool call and have nowhere to
        // send it. It is never inferred from the request's Host header, which the caller sets.
        using var app = Composed(new McpRow());

        var result = WebMcpMcpHosting.Initialize(app);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("SelfBaseAddress");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ARowNamingASelfAddressMapsTheEndpoint()
    {
        using var app = Composed(new McpRow { SelfBaseAddress = "http://127.0.0.1:5510" });

        var result = WebMcpMcpHosting.Initialize(app);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        RouteCount(app).ShouldBeGreaterThan(0);
    }

    /// <summary>How many endpoints the app has mapped.</summary>
    private static int RouteCount(WebApplication app)
        => ((IEndpointRouteBuilder)app).DataSources.SelectMany(static source => source.Endpoints).Count();

    /// <summary>A web host with both registrants and a WebMcp row, before Initialize.</summary>
    private static WebApplication Composed(McpRow row)
    {
        var configurations = new Mock<IWebMcpConfigurationProvider>();
        configurations.Setup(p => p.Get(WebMcpHosting.ConfigurationName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IWebMcpImplementationConfiguration>.Success(row));

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(configurations.Object);
        builder.Services.AddSingleton(new Mock<ISecretManagerProvider>().Object);
        WebMcpHosting.Register(builder).IsSuccess.ShouldBeTrue();
        WebMcpMcpHosting.Register(builder).IsSuccess.ShouldBeTrue();
        return builder.Build();
    }
}
