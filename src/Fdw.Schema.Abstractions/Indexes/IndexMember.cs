#pragma warning disable CS1591
namespace Fdw.Schema.Indexes;

/// <summary>
/// Represents a single member (column) of an index.
/// </summary>
/// <param name="Ordinal">The ordinal position within the index (0-based).</param>
/// <param name="PropertyName">The name of the property included in the index.</param>
/// <param name="IsDescending">True if this column is sorted descending; otherwise ascending.</param>
// Why: pure positional record (DTO), auto-generated properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public readonly record struct IndexMember(int Ordinal, string PropertyName, bool IsDescending = false);
