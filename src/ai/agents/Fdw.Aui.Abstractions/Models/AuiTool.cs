using System;

namespace Fdw.Aui.Models;

/// <summary>
/// Represents a tool (action) available to an agent within the UI.
/// </summary>
public sealed class AuiTool
{
    /// <summary>
    /// Gets or sets the unique tool name (e.g., run_pipeline).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of what the tool does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON-Schema for the tool's input parameters.
    /// </summary>
    public string InputSchema { get; set; } = "{}";

    /// <summary>
    /// Gets or sets a value indicating whether this tool requires human confirmation.
    /// </summary>
    public bool RequiresConfirmation { get; set; }
}
