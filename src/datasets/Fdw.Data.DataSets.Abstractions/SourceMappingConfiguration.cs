using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Configuration;
using FluentValidation;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Configuration for source mappings that define how to access data from different connection types.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SourceMappingConfiguration
{
    /// <summary>
    /// Gets or sets the connection name to use for this source.
    /// </summary>
    /// <value>The name of the configured connection (e.g., "SqlServerProd", "OrdersRestApi").</value>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection type name (e.g., "SQL", "HTTP", "File").
    /// </summary>
    /// <value>The type identifier for the connection that can provide this data.</value>
    public string ConnectionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DataStore name that owns this source's container.
    /// </summary>
    /// <value>The name of the registered DataStore (e.g., "OrdersDb", "CustomerApi").</value>
    /// <remarks>
    /// The DataStoreName links this source to a physical DataStore which contains
    /// the container (table, endpoint, file) being accessed. The fully qualified
    /// container path is constructed as: DataStoreName::ContainerPath
    /// (e.g., "OrdersDb::dbo.Customers").
    /// </remarks>
    public string DataStoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the priority of this source when multiple sources are available.
    /// </summary>
    /// <value>Lower values indicate higher priority (1 = highest priority).</value>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Gets or sets SQL-specific mapping configuration.
    /// </summary>
    /// <value>Configuration for SQL table/view access.</value>
    public SqlMappingConfiguration? Sql { get; set; }

    /// <summary>
    /// Gets or sets HTTP-specific mapping configuration.
    /// </summary>
    /// <value>Configuration for HTTP API access.</value>
    public HttpMappingConfiguration? Http { get; set; }

    /// <summary>
    /// Gets or sets file-specific mapping configuration.
    /// </summary>
    /// <value>Configuration for file-based data access.</value>
    public FileMappingConfiguration? File { get; set; }

    /// <summary>
    /// Gets or sets whether this source supports predicate pushdown optimization.
    /// </summary>
    /// <value>
    /// <c>true</c> if filters can be pushed down to this source; otherwise, <c>false</c>.
    /// When true, filter conditions will be translated and included in the source query.
    /// When false, all data will be fetched and filtered in memory after joining.
    /// </value>
    public bool SupportsPredicatePushdown { get; set; } = true;
}