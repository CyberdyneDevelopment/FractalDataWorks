using System;
using System.Collections.Generic;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Framework-agnostic component metadata that can be serialized to JSON.
/// Consumed by ANY JavaScript framework (Blazor, React, Vue, Angular, etc.).
/// </summary>
public class ComponentMetadata
{
    /// <summary>
    /// Type name of the component (e.g., "EmailConfigurationWebComponent").
    /// </summary>
    public string ComponentType { get; set; } = "";

    /// <summary>
    /// Type name of the model (e.g., "EmailConfiguration").
    /// </summary>
    public string ModelType { get; set; } = "";

    /// <summary>
    /// Property metadata for all properties.
    /// </summary>
    public IList<PropertyMetadata> Properties { get; set; } = new List<PropertyMetadata>();

    /// <summary>
    /// Child components (for nested structures).
    /// </summary>
    public IList<ComponentMetadata> ChildComponents { get; set; } = new List<ComponentMetadata>();

    /// <summary>
    /// Additional component-level attributes.
    /// </summary>
    public IDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <summary>
    /// Render mode ID (reference to RenderModes TypeCollection).
    /// </summary>
    public int RenderModeId { get; set; }

    /// <summary>
    /// Gets render mode name for serialization.
    /// </summary>
    public string RenderMode => RenderModes.ById(RenderModeId)?.Name ?? "View";
}
