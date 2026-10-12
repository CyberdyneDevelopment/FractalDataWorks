using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// Data returned by type hierarchy analysis.
/// </summary>
public sealed record TypeHierarchyData
{
    /// <summary>
    /// Gets or sets the type name.
    /// </summary>
    public string TypeName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the base types.
    /// </summary>
    public IReadOnlyList<TypeHierarchyEntry> BaseTypes { get; init; } = Array.Empty<TypeHierarchyEntry>();

    /// <summary>
    /// Gets or sets the implemented interfaces.
    /// </summary>
    public IReadOnlyList<TypeHierarchyEntry> Interfaces { get; init; } = Array.Empty<TypeHierarchyEntry>();

    /// <summary>
    /// Gets or sets the count of base types.
    /// </summary>
    public int BaseTypeCount { get; init; }

    /// <summary>
    /// Gets or sets the count of interfaces.
    /// </summary>
    public int InterfaceCount { get; init; }
}