using System;

namespace Fdw.Services.Connections.MsSql.Commands;

/// <summary>
/// Result of schema persistence operation.
/// </summary>
public sealed class SchemaPersistResult
{
    /// <summary>
    /// Gets the DataStore identifier (existing or newly created).
    /// </summary>
    public required Guid DataStoreId { get; init; }

    /// <summary>
    /// Gets whether a new DataStore was created.
    /// </summary>
    public required bool IsNewDataStore { get; init; }

    /// <summary>
    /// Gets the number of paths added.
    /// </summary>
    public int PathsAdded { get; init; }

    /// <summary>
    /// Gets the number of paths that already existed.
    /// </summary>
    public int PathsModified { get; init; }

    /// <summary>
    /// Gets the number of containers added.
    /// </summary>
    public int ContainersAdded { get; init; }

    /// <summary>
    /// Gets the number of containers that already existed.
    /// </summary>
    public int ContainersModified { get; init; }

    /// <summary>
    /// Gets the number of fields added.
    /// </summary>
    public int FieldsAdded { get; init; }

    /// <summary>
    /// Gets the number of fields that already existed.
    /// </summary>
    public int FieldsModified { get; init; }
}
