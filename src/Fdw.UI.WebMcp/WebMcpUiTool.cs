using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.UI.WebMcp;

/// <summary>
/// A page-scoped tool published to an in-browser AI agent through
/// <c>document.modelContext.registerTool()</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is the UI-layer counterpart to <c>Fdw.Hosting.WebMcp.WebMcpToolDescriptor</c>. That type
/// describes an HTTP endpoint and its generated <c>execute</c> callback issues a <c>fetch()</c>;
/// this type describes a tool whose <see cref="Execute"/> delegate runs in-process inside the
/// live Blazor circuit, so it can read and mutate the component state the user is looking at
/// without a second authenticated round trip.
/// </para>
/// <para>
/// Tools live exactly as long as the <c>WebMcpBridge</c> that owns them. When the bridge is
/// disposed — a page navigation in a SPA — its registrations are aborted, so an agent never sees
/// a tool for a page that is no longer on screen.
/// </para>
/// </remarks>
public sealed class WebMcpUiTool
{
    /// <summary>
    /// Gets the unique tool name the agent calls (for example <c>filter_pipelines</c>).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the natural-language description the agent uses to decide when to call this tool.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the JSON Schema for the tool's arguments, as a raw JSON object string.
    /// </summary>
    /// <remarks>
    /// Must parse to a JSON <em>object</em>. A tool taking no arguments declares
    /// <c>{"type":"object","properties":{}}</c> — it is never left null or empty, because the
    /// bridge refuses to register a tool whose schema it cannot parse rather than substituting a
    /// permissive default.
    /// </remarks>
    public required string InputSchema { get; init; }

    /// <summary>
    /// Gets the delegate that runs when the agent calls this tool, receiving the agent's parsed
    /// arguments and returning the string result handed back to the agent.
    /// </summary>
    public required Func<JsonElement, CancellationToken, Task<string>> Execute { get; init; }

    /// <summary>
    /// Gets a value indicating whether the tool only reads state, surfaced to the agent as
    /// <c>annotations.readOnlyHint</c>. Null omits the hint.
    /// </summary>
    public bool? ReadOnlyHint { get; init; }

    /// <summary>
    /// Gets a value indicating whether the tool returns content originating outside the
    /// application's trust boundary, surfaced as <c>annotations.untrustedContentHint</c>.
    /// Null omits the hint.
    /// </summary>
    public bool? UntrustedContentHint { get; init; }

    /// <summary>
    /// Gets a value indicating whether a human must approve each invocation before
    /// <see cref="Execute"/> runs.
    /// </summary>
    /// <remarks>
    /// WebMCP has no wire-level confirmation flag — whether an agent prompts its user is the
    /// agent's choice, not something the page can enforce. So the bridge enforces it locally: a
    /// tool with this set requires a confirmation handler on the bridge, and is refused (loudly,
    /// never silently executed) when none is wired.
    /// </remarks>
    public bool RequiresConfirmation { get; init; }
}
