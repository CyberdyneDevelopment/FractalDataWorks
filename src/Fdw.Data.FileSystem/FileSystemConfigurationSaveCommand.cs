using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Data.FileSystem;

/// <summary>
/// The native FileSystem command that persists a NEW logical version of a configuration record
/// (the version-on-write CREATE the config <c>ConfigurationSave</c> command emits). Carries the record
/// POCO the connection maps to columns, plus any <see cref="AdditionalColumnValues"/> (a KVP child's
/// owner FK, absent from the POCO). The <c>FileSystemConfigurationWriter</c> reads the container's
/// current rows, assigns a new physical RowId, resolves any FK RowId columns against the parent file,
/// sets <c>IsCurrent=true</c>/<c>IsDeleted=false</c> explicitly, retires any prior current version, then
/// rewrites the whole file.
/// </summary>
public sealed class FileSystemConfigurationSaveCommand : FileSystemRecordCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemConfigurationSaveCommand"/> class.
    /// </summary>
    /// <param name="relativePath">The file path relative to the connection root.</param>
    /// <param name="container">The configured container to write.</param>
    /// <param name="record">The configuration record POCO to persist.</param>
    /// <param name="additionalColumnValues">
    /// Extra column=value pairs merged into the new row beyond the POCO's mapped columns (e.g. a KVP
    /// child's owner FK), copied through unchanged from the source <c>ConfigurationSaveCommand</c>.
    /// </param>
    public FileSystemConfigurationSaveCommand(
        string relativePath,
        IDataContainer container,
        object record,
        IReadOnlyDictionary<string, object?> additionalColumnValues)
        : base(relativePath, container)
    {
        Record = record;
        AdditionalColumnValues = additionalColumnValues;
    }

    /// <summary>Gets the configuration record POCO to persist.</summary>
    public object Record { get; }

    /// <summary>Gets the extra column=value pairs merged into the new row beyond the POCO's mapped columns.</summary>
    public IReadOnlyDictionary<string, object?> AdditionalColumnValues { get; }
}
