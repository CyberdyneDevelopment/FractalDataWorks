using System;

namespace Fdw.Data.SchemaImporters.Abstractions;

/// <summary>
/// Result of a schema sync operation.
/// </summary>
public sealed class SchemaImportSyncResult
{
    /// <summary>
    /// Gets or sets the DataStore configuration ID.
    /// </summary>
    public Guid DataStoreId { get; init; }

    /// <summary>
    /// Gets or sets the number of paths added.
    /// </summary>
    public int PathsAdded { get; init; }

    /// <summary>
    /// Gets or sets the number of paths modified.
    /// </summary>
    public int PathsModified { get; init; }

    /// <summary>
    /// Gets or sets the number of paths removed.
    /// </summary>
    public int PathsRemoved { get; init; }

    /// <summary>
    /// Gets or sets the number of containers added.
    /// </summary>
    public int ContainersAdded { get; init; }

    /// <summary>
    /// Gets or sets the number of containers modified.
    /// </summary>
    public int ContainersModified { get; init; }

    /// <summary>
    /// Gets or sets the number of containers removed.
    /// </summary>
    public int ContainersRemoved { get; init; }

    /// <summary>
    /// Gets or sets the number of fields added.
    /// </summary>
    public int FieldsAdded { get; init; }

    /// <summary>
    /// Gets or sets the number of fields modified.
    /// </summary>
    public int FieldsModified { get; init; }

    /// <summary>
    /// Gets or sets the number of fields removed.
    /// </summary>
    public int FieldsRemoved { get; init; }

    /// <summary>
    /// Gets or sets the new schema hash after sync.
    /// </summary>
    public string? NewSchemaHash { get; init; }

    /// <summary>
    /// Gets the total number of changes.
    /// </summary>
    public int TotalChanges =>
        PathsAdded + PathsModified + PathsRemoved +
        ContainersAdded + ContainersModified + ContainersRemoved +
        FieldsAdded + FieldsModified + FieldsRemoved;
}