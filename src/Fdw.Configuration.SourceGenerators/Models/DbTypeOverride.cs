using System.Diagnostics.CodeAnalysis;

namespace Fdw.Configuration.SourceGenerators.Models;

/// <summary>
/// Represents a DbType override from [DbType] attribute.
/// </summary>
/// <remarks>Excluded from coverage: pure data class with no logic.</remarks>
[ExcludeFromCodeCoverage]
public sealed class DbTypeOverride
{
    /// <summary>
    /// Gets or sets the SQL type (e.g., "varchar", "nvarchar", "decimal").
    /// </summary>
    public string SqlType { get; set; } = "";

    /// <summary>
    /// Gets or sets the max length for string/binary types.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the precision for decimal types.
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// Gets or sets the scale for decimal types.
    /// </summary>
    public int? Scale { get; set; }
}