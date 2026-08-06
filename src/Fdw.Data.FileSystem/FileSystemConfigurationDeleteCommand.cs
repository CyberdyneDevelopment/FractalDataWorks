using System;
using Fdw.Data.Abstractions;

namespace Fdw.Data.FileSystem;

/// <summary>
/// The native FileSystem command that soft-deletes a configuration record (the version-on-write
/// <c>ConfigurationDelete</c> the config <c>ConfigurationDeleteCommand</c> emits). Carries the logical Id
/// of the record to retire. The <c>FileSystemConfigurationWriter</c> finds the current row whose
/// logical key equals <see cref="LogicalId"/>, sets <c>IsCurrent=false</c>/<c>IsDeleted=true</c> in place
/// (no tombstone row is added), then rewrites the whole file.
/// </summary>
public sealed class FileSystemConfigurationDeleteCommand : FileSystemRecordCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemConfigurationDeleteCommand"/> class.
    /// </summary>
    /// <param name="relativePath">The file path relative to the connection root.</param>
    /// <param name="container">The configured container to write.</param>
    /// <param name="logicalId">The logical Id of the configuration record to soft-delete.</param>
    public FileSystemConfigurationDeleteCommand(
        string relativePath,
        IDataContainer container,
        Guid logicalId)
        : base(relativePath, container)
    {
        LogicalId = logicalId;
    }

    /// <summary>Gets the logical Id of the configuration record to soft-delete.</summary>
    public Guid LogicalId { get; }
}
