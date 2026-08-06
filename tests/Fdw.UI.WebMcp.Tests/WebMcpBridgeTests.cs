using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Fdw.UI.WebMcp;
using Fdw.UI.WebMcp.Components;
using Shouldly;
using Xunit;
using TestContext = Bunit.BunitContext;

namespace Fdw.UI.WebMcp.Tests;

/// <summary>
/// bUnit tests for <see cref="WebMcpBridge"/>: what reaches the browser, and how agent
/// invocations are dispatched, confirmed, and refused.
/// </summary>
public sealed class WebMcpBridgeTests
{
    private const string ModulePath = "./_content/Fdw.UI.WebMcp/js/fdw-webmcp.js";
    private const string ObjectSchema = """{"type":"object","properties":{"status":{"type":"string"}}}""";

    // ── Harness ───────────────────────────────────────────────────────────────────

    // Why: the bridge always imports its ES module and calls register() on first render, so every
    // test needs the module stubbed with a real outcome — in Loose mode InvokeAsync would return a
    // null outcome and the bridge's structured logging would have nothing to report.
    private static TestContext CreateContext(int registered = 1)
    {
        var ctx = new TestContext();

        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.Setup<WebMcpRegistrationOutcome>("register", _ => true)
              .SetResult(new WebMcpRegistrationOutcome { Supported = true, Registered = registered });
        module.SetupVoid("unregister", _ => true);

        return ctx;
    }

    private static string RegisteredPayload(TestContext ctx)
    {
        var invocation = ctx.JSInterop.Invocations["register"][0];
        // Arguments: [handle, dotNetRef, payloadJson]
        return invocation.Arguments[2]?.ToString() ?? string.Empty;
    }

    private static Task<string> Ok(JsonElement args, CancellationToken cancellationToken) =>
        Task.FromResult("done");

    // ── Publication ───────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void PublishesDeclaredToolToTheBrowser()
    {
        using var ctx = CreateContext();

        ctx.Render<WebMcpBridge>(p => p
            .Add(b => b.Route, "/pipelines")
            .AddChildContent<WebMcpPageTool>(t => t
                .Add(x => x.Name, "filter_pipelines")
                .Add(x => x.Description, "Filter the grid.")
                .Add(x => x.InputSchema, ObjectSchema)
                .Add(x => x.OnExecute, Ok)));

        var payload = RegisteredPayload(ctx);

        payload.ShouldContain("filter_pipelines");
        payload.ShouldContain("Filter the grid.");
        // The schema must land as a nested JSON object, not as an escaped string.
        payload.ShouldContain("\"inputSchema\":{");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void EmitsAnnotationsOnlyWhenHintsAreSet()
    {
        using var ctx = CreateContext();

        ctx.Render<WebMcpBridge>(p => p
            .AddChildContent<WebMcpPageTool>(t => t
                .Add(x => x.Name, "read_grid")
                .Add(x => x.Description, "Read the grid.")
                .Add(x => x.InputSchema, ObjectSchema)
                .Add(x => x.ReadOnlyHint, true)
                .Add(x => x.OnExecute, Ok)));

        RegisteredPayload(ctx).ShouldContain("\"readOnlyHint\":true");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void OmitsAnnotationsWhenNoHintsAreSet()
    {
        using var ctx = CreateContext();

        ctx.Render<WebMcpBridge>(p => p
            .AddChildContent<WebMcpPageTool>(t => t
                .Add(x => x.Name, "read_grid")
                .Add(x => x.Description, "Read the grid.")
                .Add(x => x.InputSchema, ObjectSchema)
                .Add(x => x.OnExecute, Ok)));

        RegisteredPayload(ctx).ShouldNotContain("annotations");
    }

    // ── Fail-loud schema handling ─────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void RefusesToPublishAToolWhoseSchemaIsNotParseable()
    {
        using var ctx = CreateContext();

        ctx.Render<WebMcpBridge>(p => p
            .AddChildContent<WebMcpPageTool>(t => t
                .Add(x => x.Name, "broken")
                .Add(x => x.Description, "Bad schema.")
                .Add(x => x.InputSchema, "{ not json")
                .Add(x => x.OnExecute, Ok)));

        // Why assert absence rather than a substituted default: publishing a permissive {} schema
        // would let an agent call the tool with arguments the handler never expects.
        RegisteredPayload(ctx).ShouldNotContain("broken");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void RefusesToPublishAToolWhoseSchemaIsNotAnObject()
    {
        using var ctx = CreateContext();

        ctx.Render<WebMcpBridge>(p => p
            .AddChildContent<WebMcpPageTool>(t => t
                .Add(x => x.Name, "array_schema")
                .Add(x => x.Description, "Array schema.")
                .Add(x => x.InputSchema, "[1,2,3]")
                .Add(x => x.OnExecute, Ok)));

        RegisteredPayload(ctx).ShouldNotContain("array_schema");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Ui")]
    public void RejectsADuplicateToolName()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<WebMcpBridge>(p => p
            .AddChildContent<WebMcpPageTool>(t => t
                .Add(x => x.Name, "dupe")
                .Add(x => x.Description, "First.")
                .Add(x => x.InputSchema, ObjectSchema)
                .Add(x => x.OnExecute, Ok)));

        cut.Instance.RegisterTool(new WebMcpUiTool
        {
            Name = "dupe",
            Description = "Second.",
            InputSchema = ObjectSchema,
            Execute = Ok,
        });

        var payload = RegisteredPayload(ctx);
        payload.ShouldContain("First.");
        payload.ShouldNotContain("Second.");
    }

    // ── Agent invocation ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public async Task ExecutesAToolAndReturnsItsResult()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<WebMcpBridge>(p => p
            .AddChildContent<WebMcpPageTool>(t => t
                .Add(x => x.Name, "echo")
                .Add(x => x.Description, "Echo the status.")
                .Add(x => x.InputSchema, ObjectSchema)
                .Add(x => x.OnExecute, (JsonElement args, CancellationToken ct) =>
                    Task.FromResult(args.GetProperty("status").GetString() ?? string.Empty))));

        var result = await cut.Instance.ExecuteTool("echo", """{"status":"active"}""");

        result.ShouldBe("active");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public async Task ReturnsAnErrorPayloadForAnUnknownTool()
    {
        using var ctx = CreateContext(registered: 0);

        var cut = ctx.Render<WebMcpBridge>(p => p.Add(b => b.Route, "/empty"));

        var result = await cut.Instance.ExecuteTool("missing", "{}");

        result.ShouldContain("error");
        result.ShouldContain("missing");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public async Task ReturnsAnErrorPayloadWhenArgumentsAreNotValidJson()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<WebMcpBridge>(p => p
            .AddChildContent<WebMcpPageTool>(t => t
                .Add(x => x.Name, "echo")
                .Add(x => x.Description, "Echo.")
                .Add(x => x.InputSchema, ObjectSchema)
                .Add(x => x.OnExecute, Ok)));

        var result = await cut.Instance.ExecuteTool("echo", "{ not json");

        result.ShouldContain("error");
    }

    // ── Confirmation gate ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task RefusesAConfirmationGatedToolWhenNoHandlerIsWired()
    {
        using var ctx = CreateContext();
        var executed = false;

        var cut = ctx.Render<WebMcpBridge>(p => p
            .AddChildContent<WebMcpPageTool>(t => t
                .Add(x => x.Name, "delete_all")
                .Add(x => x.Description, "Destructive.")
                .Add(x => x.InputSchema, ObjectSchema)
                .Add(x => x.RequiresConfirmation, true)
                .Add(x => x.OnExecute, (JsonElement args, CancellationToken ct) =>
                {
                    executed = true;
                    return Task.FromResult("deleted");
                })));

        var result = await cut.Instance.ExecuteTool("delete_all", "{}");

        // Why this is the important one: the failure mode to avoid is silently downgrading a
        // confirmation-gated tool to an unguarded one because nobody wired a handler.
        executed.ShouldBeFalse("A confirmation-gated tool must never run without a handler");
        result.ShouldContain("error");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task DoesNotExecuteWhenConfirmationIsDeclined()
    {
        using var ctx = CreateContext();
        var executed = false;

        var cut = ctx.Render<WebMcpBridge>(p => p
            .Add(b => b.ConfirmationHandler, _ => Task.FromResult(false))
            .AddChildContent<WebMcpPageTool>(t => t
                .Add(x => x.Name, "delete_all")
                .Add(x => x.Description, "Destructive.")
                .Add(x => x.InputSchema, ObjectSchema)
                .Add(x => x.RequiresConfirmation, true)
                .Add(x => x.OnExecute, (JsonElement args, CancellationToken ct) =>
                {
                    executed = true;
                    return Task.FromResult("deleted");
                })));

        var result = await cut.Instance.ExecuteTool("delete_all", "{}");

        executed.ShouldBeFalse("Declining confirmation must block execution");
        result.ShouldContain("error");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ExecutesWhenConfirmationIsGranted()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<WebMcpBridge>(p => p
            .Add(b => b.ConfirmationHandler, _ => Task.FromResult(true))
            .AddChildContent<WebMcpPageTool>(t => t
                .Add(x => x.Name, "delete_all")
                .Add(x => x.Description, "Destructive.")
                .Add(x => x.InputSchema, ObjectSchema)
                .Add(x => x.RequiresConfirmation, true)
                .Add(x => x.OnExecute, (JsonElement args, CancellationToken ct) =>
                    Task.FromResult("deleted"))));

        (await cut.Instance.ExecuteTool("delete_all", "{}")).ShouldBe("deleted");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Ui")]
    public async Task ReturnsAnErrorPayloadWhenTheToolThrows()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<WebMcpBridge>(p => p
            .AddChildContent<WebMcpPageTool>(t => t
                .Add(x => x.Name, "boom")
                .Add(x => x.Description, "Throws.")
                .Add(x => x.InputSchema, ObjectSchema)
                .Add(x => x.OnExecute, (JsonElement args, CancellationToken ct) =>
                    throw new InvalidOperationException("kaboom"))));

        // Why: an exception escaping across the interop boundary reaches the agent as an opaque
        // interop failure with no server-side record — it must be caught, logged, and returned.
        (await cut.Instance.ExecuteTool("boom", "{}")).ShouldContain("error");
    }

    // ── Wiring guards ─────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Ui")]
    public void ThrowsWhenAPageToolIsDeclaredOutsideABridge()
    {
        using var ctx = CreateContext();

        Should.Throw<InvalidOperationException>(() => ctx.Render<WebMcpPageTool>(p => p
            .Add(x => x.Name, "orphan")
            .Add(x => x.Description, "No bridge.")
            .Add(x => x.InputSchema, ObjectSchema)
            .Add(x => x.OnExecute, Ok)));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Ui")]
    public void ThrowsWhenAPageToolHasNoExecuteDelegate()
    {
        using var ctx = CreateContext();

        Should.Throw<InvalidOperationException>(() => ctx.Render<WebMcpBridge>(p => p
            .AddChildContent<WebMcpPageTool>(t => t
                .Add(x => x.Name, "no_handler")
                .Add(x => x.Description, "Missing OnExecute.")
                .Add(x => x.InputSchema, ObjectSchema))));
    }
}
