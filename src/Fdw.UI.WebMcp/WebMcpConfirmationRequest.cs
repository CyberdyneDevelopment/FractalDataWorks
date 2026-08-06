namespace Fdw.UI.WebMcp;

/// <summary>
/// The confirmation prompt raised before a <see cref="WebMcpUiTool"/> marked
/// <see cref="WebMcpUiTool.RequiresConfirmation"/> is executed on the agent's behalf.
/// </summary>
/// <remarks>
/// Handed to the bridge's confirmation handler, which returns <see langword="true"/> to allow the
/// invocation and <see langword="false"/> to refuse it. The handler is where an application shows
/// its own modal — this layer deliberately ships no UI of its own so the host application keeps
/// control of how approval looks.
/// </remarks>
public sealed class WebMcpConfirmationRequest
{
    /// <summary>
    /// Gets the name of the tool the agent is attempting to call.
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// Gets the raw JSON arguments the agent supplied, for display in the approval prompt.
    /// </summary>
    public required string ArgumentsJson { get; init; }
}
