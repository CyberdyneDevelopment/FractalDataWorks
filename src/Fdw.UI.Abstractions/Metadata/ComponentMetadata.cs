using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.UI.Abstractions.RenderModeOptions;

namespace Fdw.UI.Abstractions;

/// <summary>
/// Metadata about a component.
/// Used by source generators and framework adapters.
/// </summary>
// Why: pure DTO, only auto-properties, no logic.
[ExcludeFromCodeCoverage]
public class ComponentMetadata
{
    public string ComponentType { get; set; } = "";
    public string ModelType { get; set; } = "";
    public IRenderMode? RenderMode { get; set; }
    public IReadOnlyCollection<PropertyMetadata> Properties { get; set; } = Array.Empty<PropertyMetadata>();
    public IReadOnlyCollection<ComponentMetadata> ChildComponents { get; set; } = Array.Empty<ComponentMetadata>();
}
