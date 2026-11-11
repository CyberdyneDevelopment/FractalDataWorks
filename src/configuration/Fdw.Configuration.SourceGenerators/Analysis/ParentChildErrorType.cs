using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Configuration.SourceGenerators.Analysis;

/// <summary>
/// Error types for parent/child relationship validation.
/// </summary>
/// <remarks>
/// Why not a TypeCollection: a source generator runs inside the compiler before TypeCollections exist, so it cannot load Fdw.Collections.
/// </remarks>
public sealed class ParentChildErrorType
{
    /// <summary>Parent configuration not found.</summary>
    public static readonly ParentChildErrorType ParentNotFound = new(1, nameof(ParentNotFound));

    /// <summary>Foreign key property is missing.</summary>
    public static readonly ParentChildErrorType MissingForeignKey = new(2, nameof(MissingForeignKey));

    /// <summary>Circular parent reference detected.</summary>
    public static readonly ParentChildErrorType CircularReference = new(3, nameof(CircularReference));

    private ParentChildErrorType(int id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>Gets every error type.</summary>
    public static IReadOnlyList<ParentChildErrorType> All { get; } = [ParentNotFound, MissingForeignKey, CircularReference];

    /// <summary>Gets the error type's id.</summary>
    public int Id { get; }

    /// <summary>Gets the error type's name.</summary>
    public string Name { get; }

    /// <summary>The error type with this name, or null when there is none.</summary>
    /// <param name="name">The name.</param>
    /// <returns>The error type, or null.</returns>
    public static ParentChildErrorType? ByName(string name) =>
        All.FirstOrDefault(type => string.Equals(type.Name, name, StringComparison.Ordinal));

    /// <inheritdoc />
    public override string ToString() => Name;
}
