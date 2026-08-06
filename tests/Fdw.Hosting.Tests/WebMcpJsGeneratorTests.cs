using System;
using System.Collections.Generic;
using Fdw.Hosting.WebMcp;
using Shouldly;
using Xunit;

namespace Fdw.Hosting.Tests;

/// <summary>
/// Tests for <see cref="WebMcpJsGenerator"/>, the generator behind
/// <c>/.well-known/webmcp.js</c>.
/// </summary>
public sealed class WebMcpJsGeneratorTests
{
    private sealed class StubRegistry : IWebMcpToolRegistry
    {
        public IReadOnlyList<WebMcpToolDescriptor> Tools { get; init; } = Array.Empty<WebMcpToolDescriptor>();
    }

    private static string Generate(params WebMcpToolDescriptor[] tools) =>
        new WebMcpJsGenerator(new StubRegistry { Tools = tools }).Generate();

    private static WebMcpToolDescriptor ReadOnlyGetTool() => new(
        Name: "list-connections",
        Description: "List all configured data connections",
        Route: "/api/connections",
        HttpMethod: "GET",
        ReadOnly: true,
        RequestType: null,
        ResponseType: null);

    // ── Spec currency ─────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public void RegistersAgainstTheModelContextItResolved()
    {
        var js = Generate(ReadOnlyGetTool());

        // Why assert the resolved local rather than a literal receiver: the script must not call
        // navigator.modelContext.registerTool directly. Chrome 150 deprecated that form.
        js.ShouldContain("modelContext.registerTool({");
        js.ShouldNotContain("navigator.modelContext.registerTool");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void PrefersDocumentModelContextButStillAcceptsNavigator()
    {
        var js = Generate(ReadOnlyGetTool());

        // Both forms appear only in the capability probe, document first, so one generated script
        // spans the Chrome 149-156 origin-trial window.
        js.ShouldContain("document.modelContext");
        js.ShouldContain("navigator.modelContext");
        js.IndexOf("document.modelContext", StringComparison.Ordinal)
          .ShouldBeLessThan(js.IndexOf("navigator.modelContext", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ExitsQuietlyWhenTheBrowserHasNoModelContext()
    {
        Generate(ReadOnlyGetTool()).ShouldContain("if (!modelContext) return;");
    }

    // ── Tool emission ─────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    public void EmitsTheToolNameDescriptionAndRoute()
    {
        var js = Generate(ReadOnlyGetTool());

        js.ShouldContain("\"list-connections\"");
        js.ShouldContain("\"List all configured data connections\"");
        js.ShouldContain("\"/api/connections\"");
    }

    [Fact]
    [Trait("Priority", "P2")]
    public void MarksAReadOnlyToolWithTheReadOnlyHint()
    {
        Generate(ReadOnlyGetTool()).ShouldContain("readOnlyHint: true");
    }

    [Fact]
    [Trait("Priority", "P2")]
    public void EmitsNoRegistrationsWhenNoToolsAreOptedIn()
    {
        var js = Generate();

        js.ShouldNotContain("registerTool");
        // The probe still emits, so the script stays valid and simply does nothing.
        js.ShouldContain("if (!modelContext) return;");
    }
}
