using System;
using System.Linq;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using Fdw.WebMcp.Abstractions;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace Fdw.Web.RestEndpoints.Tests.EndpointTypeOptions;

/// <summary>
/// Tests that an endpoint marked as a WebMCP tool is offered to agents exactly when it is routed.
/// </summary>
/// <remarks>
/// The mark lives on the OPTION, so declaring the endpoint and offering the tool are one act. That
/// is the property worth pinning: a tool offered for an endpoint that was never registered is a
/// route the agent will call and get a 404 from, and it cannot tell that from an empty result.
/// </remarks>
[Trait("Priority", "P0")]
[Trait("Category", "CoreFramework")]
public sealed class WebMcpToolDeclarationTests
{
    private sealed class MarkedEndpoint;

    private sealed class UnmarkedEndpoint;

    private sealed class SkippedEndpoint;

    [WebMcpTool("list_marked", "Lists the marked things.", ReadOnly = true, HttpMethod = "GET")]
    private sealed class MarkedOption(Type endpointType, string name)
        : EndpointTypeOptionBase(name, endpointType, $"The {name} endpoint.", "Test");

    private sealed class UnmarkedOption(Type endpointType, string name)
        : EndpointTypeOptionBase(name, endpointType, $"The {name} endpoint.", "Test");

    [WebMcpTool("never_offered", "Should never reach an agent.")]
    private sealed class SkippedOption(Type endpointType, string name)
        : EndpointTypeOptionBase(name, endpointType, $"The {name} endpoint.", "Test");

    private static IHostApplicationBuilder NewBuilder() => Host.CreateApplicationBuilder();

    private static WebMcpToolDeclaration? DeclarationFor(Type endpointType)
        => DeclaredWebMcpTools.Declarations.FirstOrDefault(d => d.EndpointType == endpointType);

    /// <summary>A marked option offers its endpoint as a tool when it registers.</summary>
    [Fact]
    public void RegisterDeclaresAToolForAMarkedOption()
    {
        new MarkedOption(typeof(MarkedEndpoint), "Marked").Register(NewBuilder());

        DeclarationFor(typeof(MarkedEndpoint)).ShouldNotBeNull();
    }

    /// <summary>The tool carries what the attribute said about it.</summary>
    [Fact]
    public void TheDeclarationCarriesTheAttributesValues()
    {
        new MarkedOption(typeof(MarkedEndpoint), "Marked").Register(NewBuilder());

        var declaration = DeclarationFor(typeof(MarkedEndpoint));

        declaration.ShouldNotBeNull();
        declaration.Name.ShouldBe("list_marked");
        declaration.Description.ShouldBe("Lists the marked things.");
        declaration.ReadOnly.ShouldBeTrue();
        declaration.HttpMethodOverride.ShouldBe("GET");
    }

    /// <summary>An unmarked option registers its endpoint and offers no tool.</summary>
    /// <remarks>
    /// Being routed is not on its own a reason to be offered to an agent. Most endpoints are not
    /// tools, and the mark is what separates them.
    /// </remarks>
    [Fact]
    public void RegisterDeclaresNoToolForAnUnmarkedOption()
    {
        new UnmarkedOption(typeof(UnmarkedEndpoint), "Unmarked").Register(NewBuilder());

        DeclaredEndpoints.IsDeclared(typeof(UnmarkedEndpoint)).ShouldBeTrue();
        DeclarationFor(typeof(UnmarkedEndpoint)).ShouldBeNull();
    }

    /// <summary>A skipped option offers no tool, however it is marked.</summary>
    /// <remarks>
    /// The switch that keeps an endpoint off the router has to keep it out of the tool list too, or
    /// the mechanism hands agents exactly the routes someone deliberately turned off.
    /// </remarks>
    [Fact]
    public void ASkippedOptionDeclaresNoTool()
    {
        new SkippedOption(typeof(SkippedEndpoint), "Skipped") { SkipRegistration = true }.Register(NewBuilder());

        DeclaredEndpoints.IsDeclared(typeof(SkippedEndpoint)).ShouldBeFalse();
        DeclarationFor(typeof(SkippedEndpoint)).ShouldBeNull();
    }
}
