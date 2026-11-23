using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fdw.WebMcp.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.WebMcp.Mcp.Tests;

/// <summary>
/// Which tools a caller is offered over MCP (FDW-6560): only those whose route's authorization admits them.
/// </summary>
/// <remarks>
/// Each tool is judged against its route endpoint's own authorization metadata, through the host's real
/// authorization services, so the list a caller sees is the set of routes that would not refuse them.
/// </remarks>
public class WebMcpMcpToolAccessTests
{
    private const string Permission = "items:write";
    private const string PermissionClaim = "permission";

    private static Endpoint EndpointWith(params object[] metadata)
        => new(_ => Task.CompletedTask, new EndpointMetadataCollection(metadata), "test endpoint");

    private static WebMcpToolDescriptor Guarded { get; } =
        BridgeCatalog.CreateItem with { Endpoint = EndpointWith(new AuthorizeAttribute(Permission)) };

    private static WebMcpToolDescriptor Open { get; } =
        BridgeCatalog.ListItems with { Endpoint = EndpointWith(new AllowAnonymousAttribute()) };

    private static WebMcpToolDescriptor Undeclared { get; } =
        BridgeCatalog.DeleteItem with { Endpoint = EndpointWith() };

    private static DefaultHttpContext Request(ClaimsPrincipal caller, Action<AuthorizationOptions>? fallback = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Permission, policy => policy.RequireClaim(PermissionClaim, Permission));
            fallback?.Invoke(options);
        });

        return new DefaultHttpContext { RequestServices = services.BuildServiceProvider(), User = caller };
    }

    private static ClaimsPrincipal SignedIn(params Claim[] claims)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "agent-1"), .. claims], "test"));

    private static ClaimsPrincipal Unauthenticated { get; } = new(new ClaimsIdentity());

    private static async Task<string[]> Offered(HttpContext context, params WebMcpToolDescriptor[] tools)
        => [.. (await new WebMcpMcpCatalog(new BridgeCatalog.Registry(tools), NullLogger<WebMcpMcpCatalog>.Instance)
                .ToolsFor(context, TestContext.Current.CancellationToken))
            .Select(tool => tool.Name)];

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ACallerHoldingThePermissionIsOfferedBothTools()
    {
        (await Offered(Request(SignedIn(new Claim(PermissionClaim, Permission))), Guarded, Open))
            .ShouldBe(["create_item", "list_items"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ACallerWithoutThePermissionIsOfferedOnlyTheAnonymousTool()
    {
        (await Offered(Request(SignedIn()), Guarded, Open)).ShouldBe(["list_items"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AnUnauthenticatedCallerIsOfferedOnlyTheAnonymousTool()
    {
        (await Offered(Request(Unauthenticated), Guarded, Open)).ShouldBe(["list_items"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AnEndpointThatDeclaresNothingFollowsTheHostsFallbackPolicy()
    {
        static void RequireSignIn(AuthorizationOptions options)
            => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

        (await Offered(Request(Unauthenticated, RequireSignIn), Undeclared)).ShouldBeEmpty();
        (await Offered(Request(SignedIn(), RequireSignIn), Undeclared)).ShouldBe(["delete_item"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task TheCallerIsJudgedAsTheyArrivedForEveryTool()
    {
        var caller = SignedIn(new Claim(PermissionClaim, Permission));
        var context = Request(caller);

        (await Offered(context, Open, Guarded, Open)).ShouldBe(["list_items", "create_item", "list_items"]);
        context.User.ShouldBeSameAs(caller);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AToolWithNoKnownEndpointIsOfferedToNoOne()
    {
        // Who may call it is its route's decision; with no route there is nothing to decide from.
        (await Offered(Request(SignedIn(new Claim(PermissionClaim, Permission))), BridgeCatalog.GetItem, Open))
            .ShouldBe(["list_items"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task OutsideARequestNoToolIsOffered()
    {
        (await new WebMcpMcpCatalog(new BridgeCatalog.Registry(Open), NullLogger<WebMcpMcpCatalog>.Instance)
            .ToolsFor(null, TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }
}
