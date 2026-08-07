using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.FileSystem.Logging;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Builders;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.FileSystem;

/// <summary>
/// DataStore type for local/network file system storage.
/// Files are not tabular containers — BuildContainer and BuildContainerFromSource return not-supported.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataStoreTypes), "FileSystem")]
public sealed class FileSystemDataStoreType
    : DataStoreTypeBase<DataStoreConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemDataStoreType"/> class.
    /// </summary>
    public FileSystemDataStoreType() : base(
        name: "FileSystem",
        sectionName: "FileSystem",
        displayName: "File System DataStore",
        description: "Local or network file system data store")
    {
        // Why Initialize and not Register: this wiring needs a LIVE container (it resolves the
        // domain provider and its typed-body providers), and Register runs while the container
        // is still being built. Initialize runs after Build() with a real IServiceProvider.

    }

    /// <inheritdoc />
    /// <remarks>
    /// Nothing to register: FileSystem is a body-less store, and DataStore instances are assembled by
    /// the per-transport IDataStoreBuilder (SupplyBuilder) — there is no IDataStoreFactory to register
    /// (that legacy build path was removed).
    /// </remarks>
    public override IServiceCollection Register(IServiceCollection services) => services;

    /// <inheritdoc />
    /// <remarks>
    /// Registers no typed config provider — FileSystem carries its whole configuration on the header row.
    /// </remarks>

    /// <inheritdoc />
    // Why: FileSystem is a non-SQL transport that addresses containers as FILES — it supplies the
    // FileSystemDataStoreBuilder, which builds generic DataContainer nodes (format/metadata from the
    // resolved file format) but whose physical Path is the FULL relative file path
    // ({DataPath folder}/{container name}{format.CanonicalFileExtension}) rather than the folder-only
    // GenericContainerPath. This is what lets a config header and its typed body under one DataPath
    // resolve to distinct files. Replaces the earlier GenericDataStoreBuilder wiring.
    // Why: the transport boundary owns the ConnectionTypes lookup so Fdw.Data.DataNodes (where the
    // builder lives) stays connection-agnostic. ByName on an unknown name yields the NotFound connection
    // type option, whose DefaultResponseFormat is FormatTypes.NotFound — a container with no explicit
    // Format then resolves to NotFound and fails loud in ValidateConfiguration, never a silent substitute.
    public override IDataStoreBuilder SupplyBuilder(ILogger? logger = null)
        => new FileSystemDataStoreBuilder(ConnectionTypes.ByName("FileSystem").DefaultResponseFormat, logger);


}
