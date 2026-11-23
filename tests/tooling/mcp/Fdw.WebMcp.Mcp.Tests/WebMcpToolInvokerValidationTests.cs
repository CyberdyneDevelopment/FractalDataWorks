using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.Settings;
using Fdw.WebMcp.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace Fdw.WebMcp.Mcp.Tests;

/// <summary>
/// A call that does not match the tool's description is refused before the endpoint runs (FDW-6143).
/// </summary>
public class WebMcpToolInvokerValidationTests
{
    private static Dictionary<string, JsonElement> Args(string json)
        => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json).ShouldNotBeNull();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ACallWithAWrongListItemIsRefusedAndTheEndpointNeverRuns()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var reached = 0;
        await using var app = await Started(() => reached++, cancellationToken);

        var result = await app.Services.GetRequiredService<WebMcpToolInvoker>().Invoke(
            BridgeCatalog.CreateMeeting.Name,
            Args("""{"Recipients":["a@x.io",7],"Attendees":[{"Email":"a@x.io","Seats":1}]}"""),
            cancellationToken);

        result.IsError.ShouldBe(true);
        Text(result).ShouldContain("Recipients[1]");
        reached.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ACallMissingARequiredMemberOfAListItemIsRefusedNamingItsPath()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var reached = 0;
        await using var app = await Started(() => reached++, cancellationToken);

        var result = await app.Services.GetRequiredService<WebMcpToolInvoker>().Invoke(
            BridgeCatalog.CreateMeeting.Name,
            Args("""{"Attendees":[{"Seats":1}]}"""),
            cancellationToken);

        result.IsError.ShouldBe(true);
        Text(result).ShouldContain("Attendees[0].Email");
        reached.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ACallThatMatchesReachesTheEndpoint()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var reached = 0;
        await using var app = await Started(() => reached++, cancellationToken);

        var result = await app.Services.GetRequiredService<WebMcpToolInvoker>().Invoke(
            BridgeCatalog.CreateMeeting.Name,
            Args("""{"Recipients":["a@x.io"],"Attendees":[{"Email":"a@x.io","Seats":1}]}"""),
            cancellationToken);

        result.IsError.ShouldNotBe(true, Text(result));
        reached.ShouldBe(1);
    }

    private static string Text(CallToolResult result)
        => result.Content.OfType<TextContentBlock>().Single().Text;

    private static async Task<WebApplication> Started(System.Action reached, CancellationToken cancellationToken)
    {
        var configurations = new Mock<IWebMcpConfigurationProvider>();
        configurations.Setup(p => p.Get(WebMcpHosting.ConfigurationName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IWebMcpImplementationConfiguration>.Success(new McpRow { SelfAddressSource = "ListeningAddress" }));
        var depth = BridgeCatalog.MaxDepth;
        var settings = new Mock<IEffectiveSettingsProvider>();
        settings.Setup(s => s.TryGetEffectiveValue(SettingDefinitions.WebMcpToolSchemaMaxDepth, out depth, null, null)).Returns(true);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(configurations.Object);
        builder.Services.AddSingleton(new Mock<ISecretManagerProvider>().Object);
        builder.Services.AddSingleton(settings.Object);
        builder.Services.AddSingleton<IWebMcpToolRegistry>(new BridgeCatalog.Registry(BridgeCatalog.CreateMeeting));
        WebMcpHosting.Register(builder).IsSuccess.ShouldBeTrue();
        WebMcpMcpHosting.Register(builder).IsSuccess.ShouldBeTrue();

        var app = builder.Build();
        app.MapPost(BridgeCatalog.CreateMeeting.Route, () =>
        {
            reached();
            return "booked";
        });
        WebMcpMcpHosting.Initialize(app).IsSuccess.ShouldBeTrue();
        await app.StartAsync(cancellationToken);
        return app;
    }
}
