using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.WebMcp.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Protocol;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.WebMcp.Mcp.Tests;

/// <summary>
/// Where a tool call goes (FDW-5721): two instances of one release on two ports (A/B slots) each call their
/// own routes, the request's Host header never decides it, and a host with no address is stopped.
/// </summary>
public class WebMcpSelfAddressTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TwoHostsOnTwoPortsEachCallTheirOwnRoutes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var slotA = await Started("slot-a", cancellationToken);
        await using var slotB = await Started("slot-b", cancellationToken);

        slotA.Urls.Single().ShouldNotBe(slotB.Urls.Single());
        Text(await Invoke(slotA, cancellationToken)).ShouldBe("slot-a");
        Text(await Invoke(slotB, cancellationToken)).ShouldBe("slot-b");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ARequestsHostHeaderNamingTheOtherHostDoesNotMoveTheCall()
    {
        // The caller controls the Host header, so it is never where the address comes from.
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var slotA = await Started("slot-a", cancellationToken);
        await using var slotB = await Started("slot-b", cancellationToken);
        var other = new Uri(slotB.Urls.Single());

        var forged = new DefaultHttpContext();
        forged.Request.Host = new HostString(other.Host, other.Port);
        forged.Request.Headers.Host = other.Authority;
        slotA.Services.GetRequiredService<IHttpContextAccessor>().HttpContext = forged;

        Text(await Invoke(slotA, cancellationToken)).ShouldBe("slot-a");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TheAddressIsTheOneTheServerBoundAndIsLoggedOnce()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var slot = await Started("slot-a", cancellationToken);
        var settings = await slot.Services.GetRequiredService<WebMcpHostSettingsReader>().Read(cancellationToken);

        var resolved = slot.Services.GetRequiredService<WebMcpSelfAddressResolver>().Resolve(settings.Value!);

        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        resolved.Value.ShouldBe(new Uri(slot.Urls.Single()));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AHostListeningOnTwoAddressesIsStoppedOnceItStarts()
    {
        // None is picked: the row's author listens on one address or states FixedAddress.
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = Composed("slot-a", "http://127.0.0.1:0", "http://127.0.0.2:0");
        WebMcpMcpHosting.Initialize(app).IsSuccess.ShouldBeTrue();

        await app.StartAsync(cancellationToken);

        app.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.IsCancellationRequested.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ASettingsRecordWithNoSourceIsRefusedOnAToolCall()
    {
        var resolver = new WebMcpSelfAddressResolver(new Mock<IServer>().Object);

        var result = resolver.Resolve(new WebMcpHostSettings("X-Webmcp-Key", ClientApiKey: null, SelfAddressSource: null, SelfBaseAddress: null));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Required configuration 'SelfAddressSource' is missing");
    }

    private static async Task<CallToolResult> Invoke(WebApplication app, CancellationToken cancellationToken)
    {
        var result = await app.Services.GetRequiredService<WebMcpToolInvoker>()
            .Invoke(BridgeCatalogue.ListItems.Name, arguments: null, cancellationToken);
        result.IsError.ShouldNotBe(true, Text(result));
        return result;
    }

    private static string Text(CallToolResult result)
        => result.Content.OfType<TextContentBlock>().Single().Text;

    /// <summary>A host listening on one dynamic loopback port, started, whose list route answers with its own name.</summary>
    private static async Task<WebApplication> Started(string name, CancellationToken cancellationToken)
    {
        var app = Composed(name, "http://127.0.0.1:0");
        WebMcpMcpHosting.Initialize(app).IsSuccess.ShouldBeTrue();
        await app.StartAsync(cancellationToken);
        return app;
    }

    /// <summary>A web host whose WebMcp row names the ListeningAddress source, before Initialize.</summary>
    private static WebApplication Composed(string name, params string[] urls)
    {
        var configurations = new Mock<IWebMcpConfigurationProvider>();
        configurations.Setup(p => p.Get(WebMcpHosting.ConfigurationName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IWebMcpImplementationConfiguration>.Success(new McpRow { SelfAddressSource = "ListeningAddress" }));

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(urls);
        builder.Services.AddSingleton(configurations.Object);
        builder.Services.AddSingleton(new Mock<ISecretManagerProvider>().Object);
        // Registered before WebMcpHosting, whose registry is added only when none is.
        builder.Services.AddSingleton<IWebMcpToolRegistry>(new BridgeCatalogue.Registry(BridgeCatalogue.ListItems));
        WebMcpHosting.Register(builder).IsSuccess.ShouldBeTrue();
        WebMcpMcpHosting.Register(builder).IsSuccess.ShouldBeTrue();

        var app = builder.Build();
        app.MapGet(BridgeCatalogue.ListItems.Route, () => name);
        return app;
    }
}
