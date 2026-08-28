using System;
using System.Collections.Generic;
using Fdw.WebMcp.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.WebMcp.Hosting.Tests;

/// <summary>
/// The join between what an endpoint option declared and what the application actually routes.
/// </summary>
public class WebMcpToolRegistryTests
{
    private sealed class ListUsersEndpoint { }

    private sealed class SaveUserEndpoint { }

    private sealed class UnroutedEndpoint { }

    private sealed class UserRequest { }

    private sealed class UserResponse { }

    private static WebMcpToolDeclaration Declaration(Type endpointType, string? httpMethodOverride = null)
        => new(endpointType, "a_tool", "Does a thing.", ReadOnly: false, httpMethodOverride);

    private static IReadOnlyList<WebMcpToolDescriptor> Resolve(
        IReadOnlyList<WebMcpToolDeclaration> declarations,
        RouteTable routes)
    {
        var registry = new WebMcpToolRegistry();
        registry.Resolve(declarations, routes, NullLogger.Instance);
        return registry.Tools;
    }

    [Fact]
    public void TakesTheRouteAndVerbFromTheRouteTable()
    {
        var tools = Resolve(
            [Declaration(typeof(ListUsersEndpoint))],
            new RouteTable().Add("/api/v1/users", typeof(ListUsersEndpoint), verbs: "GET"));

        tools.Count.ShouldBe(1);
        tools[0].Route.ShouldBe("/api/v1/users");
        tools[0].HttpMethod.ShouldBe("GET");
        tools[0].Name.ShouldBe("a_tool");
    }

    [Fact]
    public void GivesTheRouteALeadingSlash()
    {
        var tools = Resolve(
            [Declaration(typeof(ListUsersEndpoint))],
            new RouteTable().Add("users/me", typeof(ListUsersEndpoint), verbs: "GET"));

        tools[0].Route.ShouldBe("/users/me");
    }

    [Fact]
    public void CarriesTheRequestAndResponseTypesFromTheDefinition()
    {
        var tools = Resolve(
            [Declaration(typeof(SaveUserEndpoint))],
            new RouteTable().Add("/users", typeof(SaveUserEndpoint), typeof(UserRequest), typeof(UserResponse), "POST"));

        tools[0].RequestType.ShouldBe(typeof(UserRequest));
        tools[0].ResponseType.ShouldBe(typeof(UserResponse));
    }

    [Fact]
    public void SkipsAToolWhoseEndpointIsNotRouted()
    {
        // The option declared it, so the endpoint was switched on — a missing route is a
        // contradiction, and the tool must not be offered to an agent that would only get a 404.
        Resolve(
            [Declaration(typeof(UnroutedEndpoint))],
            new RouteTable().Add("/users", typeof(ListUsersEndpoint), verbs: "GET"))
            .ShouldBeEmpty();
    }

    [Fact]
    public void SkipsAToolWhoseEndpointMapsMoreThanOneVerb()
    {
        // Two verbs is a real question about which one the agent should call. Answering it by
        // guessing would hand the agent a working call to the wrong one.
        Resolve(
            [Declaration(typeof(SaveUserEndpoint))],
            new RouteTable().Add("/users", typeof(SaveUserEndpoint), verbs: ["POST", "PUT"]))
            .ShouldBeEmpty();
    }

    [Fact]
    public void UsesTheOverrideToChooseBetweenVerbs()
    {
        var tools = Resolve(
            [Declaration(typeof(SaveUserEndpoint), httpMethodOverride: "PUT")],
            new RouteTable().Add("/users", typeof(SaveUserEndpoint), verbs: ["POST", "PUT"]));

        tools.Count.ShouldBe(1);
        tools[0].HttpMethod.ShouldBe("PUT");
    }

    [Fact]
    public void IgnoresRoutesThatCarryNoEndpointDefinition()
    {
        Resolve(
            [Declaration(typeof(ListUsersEndpoint))],
            new RouteTable().AddUndefined("/healthz"))
            .ShouldBeEmpty();
    }

    [Fact]
    public void ResolvesEachDeclaredToolIndependently()
    {
        var tools = Resolve(
            [Declaration(typeof(ListUsersEndpoint)), Declaration(typeof(UnroutedEndpoint))],
            new RouteTable().Add("/users", typeof(ListUsersEndpoint), verbs: "GET"));

        tools.Count.ShouldBe(1);
        tools[0].Route.ShouldBe("/users");
    }
}
