using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data.SchemaImporters.Abstractions.Models;

/// <summary>
/// Imported DataStore configuration.
/// </summary>
/// <ExcludedFromCoverage>DTO with init-only properties</ExcludedFromCoverage>
[ExcludeFromCodeCoverage]
public sealed class ImportedDataStore
{
    /// <summary>
    /// Unique identifier for this DataStore.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Display name for this DataStore.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Type of DataStore (SqlServer, Rest, FileSystem, etc.).
    /// </summary>
    public string StoreType { get; init; } = string.Empty;

    /// <summary>
    /// Connection string, URL, or file path.
    /// </summary>
    public string Location { get; init; } = string.Empty;

    /// <summary>
    /// Additional configuration properties specific to the DataStore type.
    /// </summary>
    public IReadOnlyDictionary<string, object> Configuration { get; init; } = new Dictionary<string, object>(StringComparer.Ordinal);
}
