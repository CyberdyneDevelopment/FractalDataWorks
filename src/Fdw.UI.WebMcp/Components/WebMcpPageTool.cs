using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Fdw.UI.WebMcp.Components;

/// <summary>
/// Declares a single page-scoped WebMCP tool in markup, inside a <see cref="WebMcpBridge"/>.
/// </summary>
/// <remarks>
/// <para>
/// Renders nothing. It exists so a page can declare its agent surface next to the UI it drives:
/// </para>
/// <code>
/// &lt;WebMcpBridge Route="/pipelines"&gt;
///     &lt;WebMcpPageTool Name="filter_pipelines"
///                     Description="Filter the pipeline grid by status."
///                     InputSchema='{"type":"object","properties":{"status":{"type":"string"}}}'
///                     ReadOnlyHint="true"
///                     OnExecute="FilterPipelines" /&gt;
///     &lt;PipelineGrid @ref="_grid" /&gt;
/// &lt;/WebMcpBridge&gt;
/// </code>
/// <para>
/// Named <c>WebMcpPageTool</c> rather than <c>WebMcpTool</c> to stay unambiguous alongside the API
/// layer's <c>WebMcpToolAttribute</c>, which decorates endpoints for the generated
/// <c>/.well-known/webmcp.js</c>.
/// </para>
/// </remarks>
public sealed class WebMcpPageTool : ComponentBase
{
    /// <summary>
    /// Gets or sets the owning bridge, supplied by <see cref="WebMcpBridge"/>'s cascading value.
    /// </summary>
    [CascadingParameter]
    private WebMcpBridge? Bridge { get; set; }

    /// <summary>
    /// Gets or sets the unique tool name the agent calls.
    /// </summary>
    [Parameter, EditorRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the natural-language description the agent uses to choose this tool.
    /// </summary>
    [Parameter, EditorRequired]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON Schema for the tool's arguments, as a raw JSON object string.
    /// </summary>
    [Parameter, EditorRequired]
    public string InputSchema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delegate invoked when the agent calls this tool.
    /// </summary>
    [Parameter, EditorRequired]
    public Func<JsonElement, CancellationToken, Task<string>>? OnExecute { get; set; }

    /// <summary>
    /// Gets or sets whether the tool only reads state (<c>annotations.readOnlyHint</c>).
    /// </summary>
    [Parameter]
    public bool? ReadOnlyHint { get; set; }

    /// <summary>
    /// Gets or sets whether the tool returns content from outside the trust boundary
    /// (<c>annotations.untrustedContentHint</c>).
    /// </summary>
    [Parameter]
    public bool? UntrustedContentHint { get; set; }

    /// <summary>
    /// Gets or sets whether a human must approve each invocation.
    /// </summary>
    [Parameter]
    public bool RequiresConfirmation { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        var bridge = Bridge
            ?? throw new InvalidOperationException(
                $"{nameof(WebMcpPageTool)} '{Name}' must be placed inside a {nameof(WebMcpBridge)}.");

        var execute = OnExecute
            ?? throw new InvalidOperationException(
                $"{nameof(WebMcpPageTool)} '{Name}' requires {nameof(OnExecute)}.");

        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException($"{nameof(WebMcpPageTool)} requires a non-empty {nameof(Name)}.");

        if (string.IsNullOrWhiteSpace(Description))
            throw new InvalidOperationException($"{nameof(WebMcpPageTool)} '{Name}' requires a non-empty {nameof(Description)}.");

        if (string.IsNullOrWhiteSpace(InputSchema))
            throw new InvalidOperationException($"{nameof(WebMcpPageTool)} '{Name}' requires a non-empty {nameof(InputSchema)}.");

        bridge.RegisterTool(new WebMcpUiTool
        {
            Name = Name,
            Description = Description,
            InputSchema = InputSchema,
            Execute = execute,
            ReadOnlyHint = ReadOnlyHint,
            UntrustedContentHint = UntrustedContentHint,
            RequiresConfirmation = RequiresConfirmation,
        });
    }
}
