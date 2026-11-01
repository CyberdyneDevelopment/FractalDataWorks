#pragma warning disable CS1591
namespace Fdw.Schema.Keys;

/// <summary>
/// Represents a single member of a key (primary or unique).
/// </summary>
/// <param name="Ordinal">The ordinal position within the key (0-based).</param>
/// <param name="PropertyName">The name of the property included in the key.</param>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public readonly record struct KeyMember(int Ordinal, string PropertyName);
