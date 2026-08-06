using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.UI.Abstractions;

/// <summary>
/// Metadata about a property.
/// </summary>
// Why: pure DTO, only auto-properties, no logic.
[ExcludeFromCodeCoverage]
public class PropertyMetadata
{
    public string Name { get; set; } = "";
    public string PropertyType { get; set; } = "";
    public string? Label { get; set; }
    public string? HelpText { get; set; }
    public string? Group { get; set; }
    public int Order { get; set; }
    public bool Required { get; set; }
    public bool ReadOnly { get; set; }
    public object? DefaultValue { get; set; }
    public IReadOnlyDictionary<string, object> ValidationRules { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);
}
