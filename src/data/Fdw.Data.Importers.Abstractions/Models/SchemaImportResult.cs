using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data.SchemaImporters.Abstractions.Models;

/// <summary>
/// Result of a schema import operation.
/// </summary>
/// <ExcludedFromCoverage>DTO with init-only properties</ExcludedFromCoverage>
[ExcludeFromCodeCoverage]
public sealed class SchemaImportResult
{
    /// <summary>
    /// Name of the importer that produced this result.
    /// </summary>
    public string ImporterName { get; init; } = string.Empty;

    /// <summary>
    /// Source connection string, URL, or file path.
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when the import was performed.
    /// </summary>
    public DateTime ImportedAt { get; init; }

    /// <summary>
    /// Imported DataStore configuration.
    /// </summary>
    public ImportedDataStore DataStore { get; init; } = null!;

    /// <summary>
    /// List of imported containers (tables, endpoints, files, etc.).
    /// </summary>
    public IReadOnlyList<ImportedContainer> Containers { get; init; } = Array.Empty<ImportedContainer>();

    /// <summary>
    /// Warnings encountered during import.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Additional metadata collected during import.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>(StringComparer.Ordinal);
}
