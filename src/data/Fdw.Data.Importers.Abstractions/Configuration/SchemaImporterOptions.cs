using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Abstractions;

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
    /// Include extended properties (MS_Description, etc.) during SQL Server schema import. Required: the caller that asks for the import states it (FDW-3823).
    /// </summary>
    public required bool IncludeExtendedProperties { get; init; }

    /// <summary>
    /// Preferred format for REST/OData imports (defaults to JSON).
    /// </summary>
    public IFormatType? PreferredFormat { get; init; }

    /// <summary>
    /// Include deprecated endpoints in OpenAPI imports (default: false).
    /// </summary>
    public bool IncludeDeprecatedEndpoints { get; init; }

    /// <summary>
    /// Timeout for schema import operations.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Additional metadata to include in import.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <summary>
    /// Gets the provider an importer that reads through a configured connection resolves its <c>source</c> with (FDW-5951): the SQL Server
    /// importer takes the name of a configured connection as its source and asks this provider for it.
    /// </summary>
    /// <value>
    /// <see langword="null"/> when the options carry none. An importer that needs one then refuses with a message that names the missing
    /// provider; there is no other way to reach a connection and no fallback to a connection string. The OData and OpenAPI importers do not use it.
    /// </value>
    public IDataConnectionProvider? ConnectionProvider { get; init; }
}
