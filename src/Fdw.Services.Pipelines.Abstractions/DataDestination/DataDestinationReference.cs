using System;
using System.Collections.Generic;
using Fdw.Services.Pipelines.Abstractions.WriteMode;

namespace Fdw.Services.Pipelines.Abstractions.DataDestination;

/// <summary>
/// Framework-agnostic reference to a data destination.
/// Can be a Connection (ETL) or DataSet (ELT).
/// </summary>
public sealed class DataDestinationReference : IEquatable<DataDestinationReference>
{
    /// <summary>
    /// Gets or sets the kind of data destination.
    /// </summary>
    public IDataDestinationKind Kind { get; set; } = DataDestinationKinds.NotFound;

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
    /// Gets or sets the write mode for the destination.
    /// </summary>
    public IWriteMode WriteMode { get; set; } = WriteModes.NotFound;

    /// <summary>
    /// Gets or sets additional options for the destination.
    /// </summary>
    public IDictionary<string, object?>? Options { get; set; }

    /// <summary>
    /// Creates a new connection-based data destination reference.
    /// </summary>
    public static DataDestinationReference ToConnection(string connectionName, string? containerPath = null, IWriteMode? writeMode = null)
    {
        return new DataDestinationReference
        {
            Kind = DataDestinationKinds.ByName("Connection"),
            Name = connectionName,
            ContainerPath = containerPath,
            WriteMode = writeMode ?? WriteModes.ByName("Insert")
        };
    }

    /// <summary>
    /// Creates a new dataset-based data destination reference.
    /// </summary>
    public static DataDestinationReference ToDataSet(string dataSetName)
    {
        return new DataDestinationReference
        {
            Kind = DataDestinationKinds.ByName("DataSet"),
            Name = dataSetName
        };
    }

    /// <summary>
    /// Creates a deep copy of this reference.
    /// </summary>
    public DataDestinationReference Clone()
    {
        return new DataDestinationReference
        {
            Kind = Kind,
            Name = Name,
            ContainerPath = ContainerPath,
            WriteMode = WriteMode,
            Options = Options != null ? new Dictionary<string, object?>(Options, StringComparer.Ordinal) : null
        };
    }

    /// <inheritdoc />
    public bool Equals(DataDestinationReference? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return string.Equals(Kind.Name, other.Kind.Name, StringComparison.Ordinal) &&
               string.Equals(Name, other.Name, StringComparison.Ordinal) &&
               string.Equals(ContainerPath, other.ContainerPath, StringComparison.Ordinal) &&
               string.Equals(WriteMode.Name, other.WriteMode.Name, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as DataDestinationReference);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Kind.Id.GetHashCode();
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Name ?? string.Empty);
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(ContainerPath ?? string.Empty);
            hash = hash * 31 + WriteMode.Id.GetHashCode();
            return hash;
        }
    }
}
