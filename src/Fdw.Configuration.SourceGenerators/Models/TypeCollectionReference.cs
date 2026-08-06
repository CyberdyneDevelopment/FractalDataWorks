using System.Diagnostics.CodeAnalysis;

namespace Fdw.Configuration.SourceGenerators.Models;

/// <summary>
/// Represents a reference to a TypeCollection on a property.
/// </summary>
/// <remarks>Excluded from coverage: pure data class with no logic.</remarks>
[ExcludeFromCodeCoverage]
public sealed class TypeCollectionReference
{
    /// <summary>
    /// Gets or sets the full type name of the TypeCollection.
    /// </summary>
    public string TypeCollectionFullName { get; set; } = "";

    /// <summary>
    /// Gets or sets the simple name of the TypeCollection.
    /// </summary>
    public string TypeCollectionName { get; set; } = "";

    /// <summary>
    /// Gets or sets whether validation uses ById (true) or ByName (false).
    /// </summary>
    public bool ById { get; set; }

    /// <summary>
    /// Gets or sets the table name for the TypeCollection lookup table.
    /// Defaults to the TypeCollection name.
    /// </summary>
    public string TableName { get; set; } = "";

    /// <summary>
    /// Gets or sets the database schema for the TypeCollection lookup table.
    /// Defaults to "cfg".
    /// </summary>
    public string Schema { get; set; } = "cfg";
}