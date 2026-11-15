using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.DataStores.SqlServer.Catalog;
using Fdw.Data.DataStores.SqlServer.Logging;
using Fdw.Data.SchemaImporters.Abstractions;
using Fdw.Data.SchemaImporters.Abstractions.Configuration;
using Fdw.Results;
using Fdw.Services.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.DataStores.SqlServer;

/// <summary>
/// Imports the schema of a SQL Server database as a discovered <see cref="DataStoreImplementationConfiguration"/>: a path per schema, a container per
/// table, view and procedure, and a field per column or parameter (FDW-5951; before it, this importer opened SQL Server itself with SqlClient).
/// </summary>
/// <remarks>
/// <para>
/// <c>source</c> is the NAME of a configured connection, never a connection string. The importer resolves it with the
/// <see cref="Fdw.Services.Connections.Abstractions.IDataConnectionProvider"/> in <see cref="SchemaImporterOptions.ConnectionProvider"/> and reads the
/// catalog through that connection (<see cref="SqlServerCatalogImporter"/>); it opens nothing itself. A source that is empty or is a connection string,
/// options with no provider, and a name that resolves to no connection are each refused with their own message; a failure of the provider keeps its cause.
/// </para>
/// <para>
/// The importer does not check what kind of connection it was given: a connection declares no kind. A connection of another engine cannot answer the SQL
/// Server catalog, so its first catalog read fails and that failure is the import's: it is never an empty store.
/// </para>
/// <para>
/// A failed catalog read fails the import. The SqlClient importer carried on after some failed reads (a view or procedure list became a warning and an
/// empty list, a table whose columns failed was skipped, a procedure whose parameters failed had no fields).
/// </para>
/// </remarks>
[TypeOption(typeof(SchemaImporters.Abstractions.SchemaImporters), "SqlServer")]
public sealed class SqlServerSchemaImporter : SchemaImporterBase<SqlServerConfiguration>, ISchemaImporter<SqlServerConfiguration>
{
    private readonly ILogger<SqlServerSchemaImporter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerSchemaImporter"/> class without a logger. The
    /// registration module initializer builds registered options through this constructor.
    /// </summary>
    public SqlServerSchemaImporter()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerSchemaImporter"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic output.</param>
    public SqlServerSchemaImporter(ILogger<SqlServerSchemaImporter>? logger)
        : base(
            name: "SqlServer",
            description: "Imports schema from SQL Server databases through a configured connection",
            dataStoreType: "SqlServer")
    {
        _logger = logger ?? NullLogger<SqlServerSchemaImporter>.Instance;
    }

    /// <inheritdoc/>
    /// <param name="source">The name of a configured connection.</param>
    /// <param name="options">The import options; <see cref="SchemaImporterOptions.ConnectionProvider"/> resolves <paramref name="source"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public override async Task<IGenericResult<DataStoreImplementationConfiguration>> Import(
        string source,
        SchemaImporterOptions options,
        CancellationToken cancellationToken = default)
    {
        var reader = await OpenReader(source, options, cancellationToken).ConfigureAwait(false);
        if (!reader.IsSuccess || reader.Value is not { } opened)
        {
            return reader.ToNewResult<DataStoreImplementationConfiguration>();
        }

        return await new SqlServerCatalogImporter(opened, null).Import(options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates the source with no options. <see cref="ISchemaImporter.Validate"/> carries none, so there is no provider to resolve a connection
    /// name with: it refuses, naming the missing provider. Use <see cref="Validate(string, SchemaImporterOptions, CancellationToken)"/>.
    /// </summary>
    /// <param name="source">The name of a configured connection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The refusal.</returns>
    public override Task<IGenericResult<bool>> Validate(
        string source,
        CancellationToken cancellationToken = default)
        => Validate(source, null, cancellationToken);

    /// <summary>
    /// Validates the source: it names a configured connection the options' provider resolves, and the catalog of that connection can be read.
    /// </summary>
    /// <param name="source">The name of a configured connection.</param>
    /// <param name="options">The options that carry the connection provider.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True, or the refusal or the failure of the read.</returns>
    public Task<IGenericResult<bool>> Validate(
        string source,
        SchemaImporterOptions? options,
        CancellationToken cancellationToken = default)
        => ValidateWith(source, options, cancellationToken);

    private async Task<IGenericResult<bool>> ValidateWith(string source, SchemaImporterOptions? options, CancellationToken cancellationToken)
    {
        var reader = await OpenReader(source, options, cancellationToken).ConfigureAwait(false);
        if (!reader.IsSuccess || reader.Value is not { } opened)
        {
            return reader.ToNewResult<bool>();
        }

        return await new SqlServerCatalogImporter(opened, null).Validate(cancellationToken).ConfigureAwait(false);
    }

    // The source as a connection: refused when empty, when it is a connection string (it would carry a password to a log) and when the options carry
    // no provider; the provider's own failure passes up; a provider that answers no connection is refused naming the connection.
    private async Task<IGenericResult<SqlServerCatalogReader>> OpenReader(string source, SchemaImporterOptions? options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return GenericResult<SqlServerCatalogReader>.Failure(SqlServerImporterSourceLog.SourceNamesNoConnection(_logger));
        }

        if (source.Contains('=', StringComparison.Ordinal) || source.Contains(';', StringComparison.Ordinal))
        {
            return GenericResult<SqlServerCatalogReader>.Failure(SqlServerImporterSourceLog.SourceIsAConnectionString(_logger));
        }

        if (options?.ConnectionProvider is not { } provider)
        {
            return GenericResult<SqlServerCatalogReader>.Failure(SqlServerImporterSourceLog.NoConnectionProvider(_logger, source));
        }

        var resolved = await provider.Get(source, cancellationToken).ConfigureAwait(false);
        if (resolved.IsFailure)
        {
            return resolved.ToNewResult<SqlServerCatalogReader>();
        }

        if (resolved.Value is not { } connection)
        {
            return GenericResult<SqlServerCatalogReader>.Failure(SqlServerImporterSourceLog.ConnectionResolvesToNone(_logger, source));
        }

        SqlServerImporterSourceLog.ConnectionResolved(_logger, source);
        return GenericResult<SqlServerCatalogReader>.Success(new SqlServerCatalogReader(connection, null));
    }
}
