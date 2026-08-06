using System.Diagnostics.CodeAnalysis;

namespace Fdw.Configuration.SourceGenerators.Models;

/// <summary>
/// Represents a [ValuesFrom] reference on a property detected during source generation.
/// </summary>
/// <remarks>Excluded from coverage: pure data class with no logic.</remarks>
[ExcludeFromCodeCoverage]
public sealed class ValuesFromReference
{
    /// <summary>
    /// Gets or sets the full type name of the TypeCollection (including namespace).
    /// </summary>
    public string TypeCollectionFullName { get; set; } = "";

    /// <summary>
    /// Gets or sets the simple name of the TypeCollection.
    /// </summary>
    public string TypeCollectionName { get; set; } = "";

    /// <summary>
    /// Gets or sets the display property name from [ValuesFrom(DisplayProperty = "...")].
    /// Null means use the default Name property.
    /// </summary>
    public string? DisplayProperty { get; set; }
}
