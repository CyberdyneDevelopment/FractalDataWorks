using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.WebMcp.Hosting.Tests;

/// <summary>
/// What the served script actually tells an agent to do.
/// </summary>
/// <remarks>
/// Asserted against the emitted JavaScript because that string IS the contract — nothing else in the
/// process reads the descriptors, and a tool that builds the wrong URL fails as a 404 the agent
/// cannot distinguish from an empty result.
/// </remarks>
public class WebMcpJsGeneratorTests
{
    private sealed class Registry(params WebMcpToolDescriptor[] tools) : IWebMcpToolRegistry
    {
        public IReadOnlyList<WebMcpToolDescriptor> Tools { get; } = tools;
    }

    private sealed class NameRequest
    {
        public string? Name { get; set; }
    }

    private sealed class ListRequest
    {
        public string? ReferenceId { get; set; }

        public int Take { get; set; }
    }

    private static string Generate(params WebMcpToolDescriptor[] tools)
        => new WebMcpJsGenerator(new Registry(tools), NullLogger<WebMcpJsGenerator>.Instance).Generate();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void PathParameterIsSubstitutedIntoTheUrlRatherThanFetchedLiterally()
    {
        var js = Generate(new WebMcpToolDescriptor(
            "get_connection_health", "Health for one connection.",
            "/connections/{Name}/health", "GET", true, typeof(NameRequest), null));

        js.ShouldContain("\"/connections/\" + encodeURIComponent(input[\"Name\"]) + \"/health\"");
        js.ShouldNotContain("fetch(\"/connections/{Name}/health\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetToolWithARequestTypeGetsRealInputsInsteadOfAnEmptySchema()
    {
        // The old generator gated the schema on the VERB, so every GET was emitted with
        // properties: {} even when it declared a request DTO - leaving the agent no field to pass
        // the path value through, and no way to filter a list.
        var js = Generate(new WebMcpToolDescriptor(
            "list_messages", "List messages.",
            "/messages", "GET", true, typeof(ListRequest), null));

        js.ShouldContain("\"ReferenceId\"");
        js.ShouldContain("\"Take\"");
        js.ShouldContain("const q = query(input,");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void PathParameterIsRequiredAndIsNotAlsoSentInTheBody()
    {
        var js = Generate(new WebMcpToolDescriptor(
            "update_connection", "Update a connection.",
            "/connections/{Name}", "PATCH", false, typeof(NameRequest), null));

        // Structural, not a validation preference: the URL cannot be built without it.
        js.ShouldContain("required: [\"Name\"]");

        // Sending it twice lets the endpoint disagree with itself about which value binds.
        js.ShouldContain("strip(input, [\"Name\"])");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AFailedCallOffersTheValuesTheParameterSelectsFrom()
    {
        var js = Generate(new WebMcpToolDescriptor(
            "get_connection_health", "Health for one connection.",
            "/connections/{Name}/health", "GET", true, typeof(NameRequest), null,
            ParentListRoute: "/connections", ParentListToolName: "list_connections"));

        js.ShouldContain("failure.validValues = await alt.json();");
        js.ShouldContain("await fetch(\"/connections\"");
        js.ShouldContain("list_connections");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void WithNoParentListTheFailureStaysABareStatus()
    {
        // No hint is better than a guessed one - naming the wrong collection sends the agent
        // somewhere confidently useless.
        var js = Generate(new WebMcpToolDescriptor(
            "get_thing", "Get a thing.",
            "/things/{Name}", "GET", true, typeof(NameRequest), null));

        js.ShouldContain("if (!r.ok) return { error: r.status + \" \" + r.statusText };");
        js.ShouldNotContain("validValues");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public void RecoveryOnAWritingToolDoesNotSendContentTypeOnItsBodylessGet()
    {
        // The recovery call is always a GET, even for a tool that writes. Reusing the writing tool's
        // headers put Content-Type on a request with no body.
        var js = Generate(new WebMcpToolDescriptor(
            "update_connection", "Update a connection.",
            "/connections/{Name}", "PATCH", false, typeof(NameRequest), null,
            ParentListRoute: "/connections", ParentListToolName: "list_connections"));

        js.ShouldContain("await fetch(\"/connections\", { method: \"GET\", headers: { \"Accept\": \"application/json\" } })");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void RecoveryReusesTheCallersHeadersSoItCannotWidenWhatTheyMaySee()
    {
        var js = Generate(new WebMcpToolDescriptor(
            "get_connection_health", "Health for one connection.",
            "/connections/{Name}/health", "GET", true, typeof(NameRequest), null,
            ParentListRoute: "/connections", ParentListToolName: "list_connections"));

        // The recovery fetch carries the same header literal as the call that failed, so it runs as
        // the same principal and can never surface values the caller could not have listed.
        js.ShouldContain("await fetch(\"/connections\", { method: \"GET\", headers: { \"Accept\": \"application/json\" } })");
    }
}
