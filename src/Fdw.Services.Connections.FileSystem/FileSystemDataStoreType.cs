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
    /// Nothing to register: FileSystem is a body-less store (see <see cref="RegisterFactory"/>), and
    /// DataStore instances are assembled by the per-transport IDataStoreBuilder (SupplyBuilder) — there
    /// is no IDataStoreFactory to register (that legacy build path was removed).
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


    /// <inheritdoc />
    // Why this is NOT a ServiceTypeBase phase: IDataStoreType declares its own RegisterFactory
    // contract, driven by ConfigurationGatewayDataStoreProvider against IDataStoreProvider — a
    // different mechanism from the option phases, and it stays.
    public override void RegisterFactory(IDataStoreProvider provider, IServiceProvider services)
    {

        var loggerFactory = services.GetService<ILoggerFactory>();
        // Why: hold a real logger so the registration milestone/failure partials below actually emit;
        // NullLogger<T>.Instance is the only sanctioned ?? fallback.
        var typeLogger = loggerFactory?.CreateLogger<FileSystemDataStoreType>() ?? NullLogger<FileSystemDataStoreType>.Instance;

        // Why NO typed provider: FileSystem is a TRANSPORT, and a transport is not what varies here.
        // data.FileSystemDataStore carries identity + audit columns and nothing else — no payload, and
        // zero rows in devConfigurationDb — because there is nothing store-specific about "the file
        // system". That makes FileSystem a body-less store today, exactly like Http, and it takes the
        // documented rule in DataStoreConfigurationProvider.OnNoTypedProvider: the header IS the
        // complete configuration.
        //
        // Registering an empty body keyed on the transport would model the wrong axis. A file store's
        // variation is its FORMAT: a structured file (Json/Xml/Parquet) is self-describing and behaves
        // like MsSqlDataStore — discovered schema metadata — while a delimited file is NOT
        // self-describing and needs its own type carrying delimiter/quote/header-row. Those are two
        // different typed bodies selected by format, not one selected by transport. The container
        // already owns the structured half (Format, RecordSelector, FlattenNestedObjects,
        // FlattenSeparator); the delimited half has no configuration surface anywhere yet
        // (DelimitedRowWriterOptions is a writer option class, not persisted configuration), so
        // inventing a transport-keyed body now would entrench the wrong shape.
        //
        // The header's own Paths collection is populated by the generic child cascade
        // (DefaultConfigurationProvider.ComposeChildren) with no typed provider involved.

        // Why: no factory registration — DataStore instances are assembled by the per-transport
        // IDataStoreBuilder (SupplyBuilder), not an IDataStoreFactory (that legacy path was removed).
        FileSystemDataStoreTypeLog.RegistrationCompleted(typeLogger, Name);
    }

}
