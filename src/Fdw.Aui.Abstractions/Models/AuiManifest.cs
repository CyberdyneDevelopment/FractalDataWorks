using System;
using System.Collections.Generic;
using Fdw.Aui.Models;

namespace Fdw.Aui.Models;

/// <summary>
/// Represents the semantic map of a UI for an AI agent.
/// Follows Google's A2UI and WebMCP standards.
/// </summary>
public sealed class AuiManifest
{
    /// <summary>
    /// Gets or sets the route or page name this manifest represents.
    /// </summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable description of the page's purpose.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of tools (actions) available on this page.
    /// </summary>
    public IReadOnlyList<AuiTool> Tools { get; set; } = Array.Empty<AuiTool>();

    /// <summary>
    /// Gets or sets the collection of resources (data) available on this page.
    /// </summary>
    public IReadOnlyList<AuiResource> Resources { get; set; } = Array.Empty<AuiResource>();

    /// <summary>
    /// Gets or sets the context variables relevant to this page.
    /// </summary>
    public IReadOnlyDictionary<string, object> Context { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
}
