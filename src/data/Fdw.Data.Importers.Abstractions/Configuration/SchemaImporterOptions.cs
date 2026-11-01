using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data.Abstractions;

namespace Fdw.Data.SchemaImporters.Abstractions.Configuration;

/// <summary>
/// Options for schema import operations.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SchemaImporterOptions
{
    /// <summary>
    /// Schema names to include (SQL Server, OData namespaces).
    /// </summary>
    public IReadOnlyList<string>? IncludeSchemas { get; init; }

    /// <summary>
    /// Schema names to exclude (SQL Server system schemas, etc.).
    /// </summary>
    public IReadOnlyList<string>? ExcludeSchemas { get; init; }

    /// <summary>
    /// Patterns to include (e.g., "dbo.*", "api/v1/*").
    /// </summary>
    public ICollection<string> IncludePatterns { get; init; } = new List<string>();

    /// <summary>
    /// Patterns to exclude (e.g., "sys.*", "temp_*").
    /// </summary>
    public ICollection<string> ExcludePatterns { get; init; } = new List<string>();

    /// <summary>
    /// Maximum number of containers to import (for testing/preview).
    /// </summary>
    public int? MaxContainers { get; init; }

    /// <summary>
    /// Import system/internal containers (default: false).
    /// </summary>
    public bool IncludeSystemContainers { get; init; }

    /// <summary>
    /// Skip views during SQL Server schema import (default: false).
    /// </summary>
    public bool SkipViews { get; init; }

    /// <summary>
    /// Skip stored procedures during SQL Server schema import (default: false).
    /// </summary>
    public bool SkipStoredProcedures { get; init; }

    /// <summary>
    /// Include row counts for SQL Server tables (requires additional queries, default: false).
    /// </summary>
    public bool IncludeRowCounts { get; init; }

    /// <summary>
    /// Include extended properties (MS_Description, etc.) during SQL Server schema import (default: true).
    /// </summary>
    public bool IncludeExtendedProperties { get; init; } = true;

    /// <summary>
    /// Preferred format for REST/OData imports (defaults to JSON).
    /// </summary>
    public IFormatType? PreferredFormat { get; init; }

    /// <summary>
    /// Include deprecated endpoints in OpenAPI imports (default: false).
    /// </summary>
    public bool IncludeDeprecatedEndpoints { get; init; }

    /// <summary>
    /// Maximum number of concurrent discovery operations (default: 10).
    /// </summary>
    public int MaxConcurrentDiscoveries { get; init; } = 10;

    /// <summary>
    /// Timeout for schema import operations.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Additional metadata to include in import.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>(StringComparer.Ordinal);
}
