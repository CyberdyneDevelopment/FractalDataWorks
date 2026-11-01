using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Configuration;
using FluentValidation;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// SQL-specific mapping configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SqlMappingConfiguration
{
    /// <summary>
    /// Gets or sets the database schema name.
    /// </summary>
    /// <value>The schema containing the table or view.</value>
    public string? SchemaName { get; set; }

    /// <summary>
    /// Gets or sets the table or view name.
    /// </summary>
    /// <value>The name of the table or view containing the data.</value>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets custom field mappings from dataset fields to database columns.
    /// </summary>
    /// <value>A dictionary mapping dataset field names to database column names.</value>
    public IDictionary<string, string> FieldMappings { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}