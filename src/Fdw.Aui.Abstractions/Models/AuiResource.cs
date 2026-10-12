using System;

namespace Fdw.Aui.Models;

/// <summary>
/// Represents a readable resource (data) available to an agent within the UI.
/// </summary>
public sealed class AuiResource
{
    /// <summary>
    /// Gets or sets the unique URI for the resource (e.g., mcp://ui/grid/pipelines).
    /// </summary>
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the resource.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MIME type of the resource content.
    /// </summary>
    public string MimeType { get; set; } = "application/json";

    /// <summary>
    /// Gets or sets the description of the resource.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
