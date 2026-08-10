using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Describes an available field transform type from the DataTransformerTypes TypeCollection.
/// </summary>
public sealed class TransformTypePayload
{
    /// <summary>Gets or sets the transform type name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the category (DateTime, Numeric, String, Boolean, Injection, Conditional).</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this transform supports column-at-a-time batch execution.</summary>
    public bool SupportsBatching { get; set; }

    /// <summary>Gets or sets the parameter definitions this transform accepts.</summary>
    public IReadOnlyList<TransformParameterDefinitionPayload> Parameters { get; set; } = [];
}
