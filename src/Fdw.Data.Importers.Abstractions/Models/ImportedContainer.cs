using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data.SchemaImporters.Abstractions.Models;

/// <summary>
/// Imported Container (table, endpoint, file, etc.).
/// </summary>
/// <ExcludedFromCoverage>DTO with init-only properties</ExcludedFromCoverage>
[ExcludeFromCodeCoverage]
public sealed class ImportedContainer
{
    /// <summary>
    /// Unique identifier for this container.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Container name (table name, endpoint path, file name, etc.).
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Schema or namespace (e.g., "dbo", "api/v1", etc.).
    /// </summary>
    public string Schema { get; init; } = string.Empty;

    /// <summary>
    /// Full qualified path to the container.
    /// </summary>
    public string FullPath { get; init; } = string.Empty;

    /// <summary>
    /// Fields/columns in this container.
    /// </summary>
    public IReadOnlyList<ImportedField> Fields { get; init; } = Array.Empty<ImportedField>();

    /// <summary>
    /// Additional metadata for this container.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>(StringComparer.Ordinal);
}
