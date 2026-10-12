namespace Fdw.UI.Pipelines.Clients.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Framework-agnostic reference to a data destination.
/// Can be a Connection (ETL) or DataSet (ELT).
/// </summary>
public sealed class DataDestinationReference : IEquatable<DataDestinationReference>
{
    /// <summary>
    /// Gets or sets the kind of data destination.
    /// </summary>
    public IDataDestinationKind Kind { get; set; } = DataDestinationKinds.Connection;

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
    public IWriteMode WriteMode { get; set; } = WriteModes.Insert;

    /// <summary>
    /// Gets or sets additional options for the destination.
    /// </summary>
    public IDictionary<string, object?>? Options { get; set; }

    /// <summary>
    /// Creates a new connection-based data destination reference.
    /// </summary>
    public static DataDestinationReference ToConnection(string connectionName, string? containerPath = null)
    {
        return new DataDestinationReference
        {
            Kind = DataDestinationKinds.Connection,
            Name = connectionName,
            ContainerPath = containerPath
        };
    }

    /// <summary>
    /// Creates a new connection-based data destination reference with an explicit write mode.
    /// </summary>
    public static DataDestinationReference ToConnection(string connectionName, string? containerPath, IWriteMode writeMode)
    {
        return new DataDestinationReference
        {
            Kind = DataDestinationKinds.Connection,
            Name = connectionName,
            ContainerPath = containerPath,
            WriteMode = writeMode
        };
    }

    /// <summary>
    /// Creates a new dataset-based data destination reference.
    /// </summary>
    public static DataDestinationReference ToDataSet(string dataSetName)
    {
        return new DataDestinationReference
        {
            Kind = DataDestinationKinds.DataSet,
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
            Options = Options != null ? new Dictionary<string, object?>(Options, StringComparer.Ordinal) : null,
        };
    }

    /// <inheritdoc />
    public bool Equals(DataDestinationReference? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Kind.Id == other.Kind.Id &&
               string.Equals(Name, other.Name, StringComparison.Ordinal) &&
               string.Equals(ContainerPath, other.ContainerPath, StringComparison.Ordinal) &&
               WriteMode.Id == other.WriteMode.Id;
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
            hash = hash * 31 + (Name != null ? StringComparer.Ordinal.GetHashCode(Name) : 0);
            hash = hash * 31 + (ContainerPath != null ? StringComparer.Ordinal.GetHashCode(ContainerPath) : 0);
            hash = hash * 31 + WriteMode.Id.GetHashCode();
            return hash;
        }
    }
}
