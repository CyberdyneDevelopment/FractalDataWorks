using System;
using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Abstractions.DataSource;

/// <summary>
/// Framework-agnostic reference to a data source.
/// Can be a Connection (ETL) or DataSet (ELT).
/// </summary>
public sealed class DataSourceReference : IEquatable<DataSourceReference>
{
    /// <summary>
    /// Gets or sets the kind of data source.
    /// </summary>
    public IDataSourceKind Kind { get; set; } = DataSourceKinds.NotFound;

    /// <summary>
    /// Gets or sets the name of the connection or dataset.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the container path (schema.table, /api/endpoint, or file path).
    /// Only applicable for Connection kind.
    /// </summary>
    public string? ContainerPath { get; set; }

    /// <summary>
    /// Gets or sets an optional alias for this source.
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// Gets or sets additional options for the source.
    /// </summary>
    public IDictionary<string, object?>? Options { get; set; }

    /// <summary>
    /// Creates a new connection-based data source reference.
    /// </summary>
    public static DataSourceReference FromConnection(string connectionName, string? containerPath = null)
    {
        return new DataSourceReference
        {
            Kind = DataSourceKinds.ByName("Connection"),
            Name = connectionName,
            ContainerPath = containerPath
        };
    }

    /// <summary>
    /// Creates a new dataset-based data source reference.
    /// </summary>
    public static DataSourceReference FromDataSet(string dataSetName)
    {
        return new DataSourceReference
        {
            Kind = DataSourceKinds.ByName("DataSet"),
            Name = dataSetName
        };
    }

    /// <summary>
    /// Creates a deep copy of this reference.
    /// </summary>
    public DataSourceReference Clone()
    {
        return new DataSourceReference
        {
            Kind = Kind,
            Name = Name,
            ContainerPath = ContainerPath,
            Alias = Alias,
            Options = Options != null ? new Dictionary<string, object?>(Options, StringComparer.Ordinal) : null
        };
    }

    /// <inheritdoc />
    public bool Equals(DataSourceReference? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return string.Equals(Kind.Name, other.Kind.Name, StringComparison.Ordinal) &&
               string.Equals(Name, other.Name, StringComparison.Ordinal) &&
               string.Equals(ContainerPath, other.ContainerPath, StringComparison.Ordinal) &&
               string.Equals(Alias, other.Alias, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as DataSourceReference);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Kind.Id.GetHashCode();
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Name ?? string.Empty);
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(ContainerPath ?? string.Empty);
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Alias ?? string.Empty);
            return hash;
        }
    }
}
