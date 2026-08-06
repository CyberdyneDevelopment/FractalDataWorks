using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Fdw.UI.WebMcp.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace Fdw.UI.WebMcp.Components;

/// <summary>
/// Publishes the tools declared by its child content to the browser's WebMCP model context, so an
/// in-browser AI agent can drive the page the user is currently looking at.
/// </summary>
/// <remarks>
/// <para>
/// This is the UI layer's entry point, and the counterpart to the API layer's
/// <c>/.well-known/webmcp.js</c>. The generated API script publishes endpoint tools whose
/// <c>execute</c> issues a <c>fetch()</c>; this component publishes component tools whose
/// <c>execute</c> calls back into the live circuit, so a tool can manipulate the very component
/// tree on screen and reuse the user's existing session with no second authentication hop.
/// </para>
/// <para>
/// Registrations are scoped to the component's lifetime. Each bridge owns a handle and a matching
/// <c>AbortController</c> in JS; <see cref="DisposeAsync"/> aborts it, so navigating away in a SPA
/// removes the page's tools instead of leaving an agent holding stale ones.
/// </para>
/// </remarks>
// Why: Blazor lifecycle methods and JS interop callbacks run on the renderer's synchronisation
// context — ConfigureAwait(false) would move continuations off it and break StateHasChanged.
[SuppressMessage("Meziantou.Analyzer", "MA0004",
    Justification = "Blazor lifecycle and interop callbacks run on the renderer sync context")]
public sealed partial class WebMcpBridge : ComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./_content/Fdw.UI.WebMcp/js/fdw-webmcp.js";

    // ── Parameters ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the content whose components declare tools against this bridge.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the route label recorded in the registration log entry.
    /// </summary>
    // Why: a log label only — it never selects behaviour, so an unset value costs nothing but a
    // less specific log line. Nothing downstream reads it.
    [Parameter]
    public string Route { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional logger. Falls back to <see cref="NullLogger{T}.Instance"/>.
    /// </summary>
    // Why: NullLogger fallback is the only acceptable ?? fallback per FDW conventions.
    [Parameter]
    public ILogger? Logger { get; set; }

    /// <summary>
    /// Gets or sets the handler consulted before executing a tool marked
    /// <see cref="WebMcpUiTool.RequiresConfirmation"/>. Returns <see langword="true"/> to allow.
    /// </summary>
    /// <remarks>
    /// A confirmation-gated tool with no handler wired is refused outright — the bridge never
    /// silently downgrades to executing it.
    /// </remarks>
    [Parameter]
    public Func<WebMcpConfirmationRequest, Task<bool>>? ConfirmationHandler { get; set; }

    // ── Injected ──────────────────────────────────────────────────────────────────

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    // ── State ─────────────────────────────────────────────────────────────────────

    private readonly List<WebMcpUiTool> _tools = [];
    private readonly CancellationTokenSource _lifetime = new();
    private readonly string _handle = Guid.CreateVersion7().ToString("N", CultureInfo.InvariantCulture);

    private IJSObjectReference? _module;
    private DotNetObjectReference<WebMcpBridge>? _selfRef;
    private bool _publishPending;
    private bool _disposed;

    private ILogger ResolvedLogger => Logger ?? NullLogger<WebMcpBridge>.Instance;

    // ── Tool declaration ──────────────────────────────────────────────────────────

    /// <summary>
    /// Declares a tool on this bridge. Call during a child component's initialisation; the batch
    /// is published to the browser after the bridge's next render.
    /// </summary>
    /// <param name="tool">The tool to publish.</param>
    public void RegisterTool(WebMcpUiTool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (FindTool(tool.Name) is not null)
        {
            // Why: two tools sharing a name would make the agent's call ambiguous — reject the
            // duplicate loudly rather than silently letting one shadow the other.
            WebMcpUiLog.DuplicateToolName(ResolvedLogger, tool.Name);
            return;
        }

        _tools.Add(tool);
        _publishPending = true;
    }

    /// <summary>
    /// Removes a previously declared tool. Takes effect on the next publish.
    /// </summary>
    /// <param name="name">The tool name to remove.</param>
    public void UnregisterTool(string name)
    {
        var tool = FindTool(name);
        if (tool is null)
            return;

        _tools.Remove(tool);
        _publishPending = true;
    }

    /// <summary>
    /// Re-publishes the current tool set to the browser, replacing the previous generation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the browser has been updated.</returns>
    public Task Refresh(CancellationToken cancellationToken = default) => Publish(cancellationToken);

    // ── Lifecycle ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed || !_publishPending)
            return;

        _publishPending = false;
        await Publish(_lifetime.Token);
    }

    // ── Publication ───────────────────────────────────────────────────────────────

    private async Task Publish(CancellationToken cancellationToken)
    {
        if (_module is null)
            _module = await JS.InvokeAsync<IJSObjectReference>("import", cancellationToken, ModulePath);

        if (_selfRef is null)
            _selfRef = DotNetObjectReference.Create(this);

        LogOutcome(await _module.InvokeAsync<WebMcpRegistrationOutcome>(
            "register", cancellationToken, _handle, _selfRef, BuildPayload()));
    }

    private void LogOutcome(WebMcpRegistrationOutcome outcome)
    {
        if (!outcome.Supported)
        {
            // Why: no model context is the ordinary case outside the Chrome origin trial — it is a
            // browser capability report, not an application failure, so it logs at Debug.
            WebMcpUiLog.ModelContextUnavailable(ResolvedLogger, _tools.Count);
            return;
        }

        foreach (var failure in outcome.Failed)
        {
            WebMcpUiLog.RegistrationRejected(ResolvedLogger, failure.Name, failure.Error);
        }

        WebMcpUiLog.ToolsRegistered(ResolvedLogger, _handle, outcome.Registered, Route);
    }

    private string BuildPayload()
    {
        var array = new JsonArray();

        foreach (var tool in _tools)
        {
            var schema = ParseSchema(tool);
            if (schema is null)
                continue;

            var node = new JsonObject
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["inputSchema"] = schema,
            };

            var annotations = BuildAnnotations(tool);
            if (annotations is not null)
                node["annotations"] = annotations;

            array.Add(node);
        }

        return array.ToJsonString();
    }

    private JsonNode? ParseSchema(WebMcpUiTool tool)
    {
        JsonNode? parsed;

        try
        {
            parsed = JsonNode.Parse(tool.InputSchema);
        }
        catch (JsonException ex)
        {
            // Why fail-loud, not a default schema: substituting {} would publish a tool that
            // accepts anything, so the agent would call it with arguments the handler never
            // expects. Refusing to register it is the safe, visible outcome.
            WebMcpUiLog.InvalidInputSchema(ResolvedLogger, tool.Name, ex.Message);
            return null;
        }

        if (parsed is JsonObject)
            return parsed;

        WebMcpUiLog.InvalidInputSchema(ResolvedLogger, tool.Name, "the parsed schema is not a JSON object");
        return null;
    }

    private static JsonObject? BuildAnnotations(WebMcpUiTool tool)
    {
        if (tool.ReadOnlyHint is null && tool.UntrustedContentHint is null)
            return null;

        var annotations = new JsonObject();

        if (tool.ReadOnlyHint is { } readOnlyHint)
            annotations["readOnlyHint"] = readOnlyHint;

        if (tool.UntrustedContentHint is { } untrustedContentHint)
            annotations["untrustedContentHint"] = untrustedContentHint;

        return annotations;
    }

    // ── Agent invocation ──────────────────────────────────────────────────────────

    /// <summary>
    /// Invoked from JS when an in-browser agent calls one of this bridge's tools.
    /// </summary>
    /// <param name="name">The tool name the agent called.</param>
    /// <param name="argumentsJson">The agent's arguments, as a JSON object string.</param>
    /// <returns>The tool's result string, or a JSON error payload.</returns>
    [JSInvokable]
    public async Task<string> ExecuteTool(string name, string argumentsJson)
    {
        WebMcpUiLog.ToolInvoked(ResolvedLogger, name);

        var tool = FindTool(name);
        if (tool is null)
            return ErrorPayload(WebMcpUiLog.ToolNotFound(ResolvedLogger, name).Message);

        if (tool.RequiresConfirmation)
        {
            var handler = ConfirmationHandler;

            if (handler is null)
                return ErrorPayload(WebMcpUiLog.ConfirmationHandlerMissing(ResolvedLogger, name).Message);

            if (!await handler(new WebMcpConfirmationRequest { ToolName = name, ArgumentsJson = argumentsJson }))
                return ErrorPayload(WebMcpUiLog.ConfirmationDeclined(ResolvedLogger, name).Message);
        }

        return await Invoke(tool, argumentsJson);
    }

    private async Task<string> Invoke(WebMcpUiTool tool, string argumentsJson)
    {
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(argumentsJson);
        }
        catch (JsonException ex)
        {
            return ErrorPayload(WebMcpUiLog.InvalidArguments(ResolvedLogger, tool.Name, ex.Message).Message);
        }

        using (document)
        {
            try
            {
                return await tool.Execute(document.RootElement, _lifetime.Token);
            }
            catch (Exception ex)
            {
                // Why catch broadly: this is the JS interop boundary. An escaping exception would
                // surface to the agent as an opaque interop failure with no server-side record, so
                // every failure is logged here and returned as a structured payload instead.
                return ErrorPayload(WebMcpUiLog.ToolExecutionFailed(ResolvedLogger, ex, tool.Name).Message);
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private WebMcpUiTool? FindTool(string name) =>
        _tools.Find(t => string.Equals(t.Name, name, StringComparison.Ordinal));

    private static string ErrorPayload(string message) =>
        new JsonObject { ["error"] = message }.ToJsonString();

    // ── Teardown ──────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await _lifetime.CancelAsync();

        if (_module is not null)
        {
            try
            {
                // Why: abort the page's registrations before releasing the module reference, so an
                // agent cannot call a tool belonging to a component that is already gone.
                await _module.InvokeVoidAsync("unregister", _handle);
                WebMcpUiLog.ToolsUnregistered(ResolvedLogger, _handle);
            }
            catch (JSDisconnectedException ex)
            {
                WebMcpUiLog.TeardownInterrupted(ResolvedLogger, ex);
            }
            catch (OperationCanceledException ex)
            {
                WebMcpUiLog.TeardownInterrupted(ResolvedLogger, ex);
            }

            await _module.DisposeAsync();
            _module = null;
        }

        _selfRef?.Dispose();
        _selfRef = null;
        _lifetime.Dispose();
    }
}
