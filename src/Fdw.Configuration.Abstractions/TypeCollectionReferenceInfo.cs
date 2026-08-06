using System.Diagnostics.CodeAnalysis;

namespace Fdw.Configuration;

/// <summary>
/// Information about a TypeCollection reference on a configuration property.
/// Used to track which properties reference TypeCollection types for validation
/// and UI generation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TypeCollectionReferenceInfo
{
    /// <summary>
    /// Gets or sets the property name.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the TypeCollection name.
    /// </summary>
    public string TypeCollectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full type name of the TypeCollection.
    /// </summary>
    public string TypeCollectionFullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether validation uses ById (true) or ByName (false).
    /// </summary>
    public bool ById { get; set; }
}
