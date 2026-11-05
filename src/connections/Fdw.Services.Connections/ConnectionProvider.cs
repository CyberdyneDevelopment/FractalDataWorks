using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Logging;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.DocumentRecords;
using Fdw.Services.Results;
using Fdw.Services.Connections.Logging;
using Fdw.ServiceTypes;

namespace Fdw.Services.Connections;

/// <summary>
/// Resolves connections by name or id, and exposes them as data connections.
/// </summary>
/// <remarks>
/// The domain configuration provider supplies the implementation configuration; the factory registered
/// for that Implementation builds the connection. Nothing is cached — the connection wraps a driver
/// that pools underneath, so a cache above it would add a disposal lifecycle and a staleness dance for
/// nothing.
/// </remarks>
public sealed class ConnectionProvider
    : DomainServiceProviderBase<
          IGenericConnection,
          IConnectionImplementationConfiguration,
          IServiceFactory<IGenericConnection>,
          IConnectionConfigurationProvider>,
      IConnectionProvider,
      IDataConnectionProvider,
      IDocumentConnectionProvider,
      IDocumentRecordConnectionProvider
{

    private readonly ILogger<ConnectionProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ConnectionProvider(
        ILogger<ConnectionProvider> logger)
        : base(logger ?? NullLogger<ConnectionProvider>.Instance)
    {
        _logger = logger ?? NullLogger<ConnectionProvider>.Instance;
    }

    // IDataConnectionProvider — every Get, cast to IDataConnection

    async Task<IGenericResult<IDataConnection>> IDataConnectionProvider.Get(string name, CancellationToken cancellationToken)
        => Cast<IDataConnection>(await Get(name, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<IDataConnection>> IDataConnectionProvider.Get(IGenericConfiguration configuration, CancellationToken cancellationToken)
        => Cast<IDataConnection>(await Get(configuration, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<IDataConnection>> IDataConnectionProvider.Get(Guid id, CancellationToken cancellationToken)
        => Cast<IDataConnection>(await Get(id, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<IReadOnlyList<IDataConnection>>> IDataConnectionProvider.Get(CancellationToken cancellationToken)
    {
        var result = await Get(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null) return result.ToNewResult<IReadOnlyList<IDataConnection>>();
        return GenericResult<IReadOnlyList<IDataConnection>>.Success(result.Value.OfType<IDataConnection>().ToList());
    }

    async Task<IGenericResult<T>> IDataConnectionProvider.Get<T>(string name, CancellationToken cancellationToken)
        => Cast<T>(await Get(name, cancellationToken).ConfigureAwait(false));

    // IDocumentConnectionProvider: every Get, cast to IDocumentConnection (FDW-1496)

    async Task<IGenericResult<IDocumentConnection>> IDocumentConnectionProvider.Get(string name, CancellationToken cancellationToken)
        => Cast<IDocumentConnection>(await Get(name, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<IDocumentConnection>> IDocumentConnectionProvider.Get(Guid id, CancellationToken cancellationToken)
        => Cast<IDocumentConnection>(await Get(id, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<IReadOnlyList<IDocumentConnection>>> IDocumentConnectionProvider.Get(CancellationToken cancellationToken)
    {
        var result = await Get(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null) return result.ToNewResult<IReadOnlyList<IDocumentConnection>>();
        return GenericResult<IReadOnlyList<IDocumentConnection>>.Success(result.Value.OfType<IDocumentConnection>().ToList());
    }

    // IDocumentRecordConnectionProvider: every Get, answered as a DocumentRecord connection, itself or through its kind's extension (FDW-3673)

    async Task<IGenericResult<IDocumentRecordConnection>> IDocumentRecordConnectionProvider.Get(string name, CancellationToken cancellationToken)
        => AsDocumentRecordConnection(await Get(name, cancellationToken).ConfigureAwait(false));

    async Task<IGenericResult<IDocumentRecordConnection>> IDocumentRecordConnectionProvider.Get(Guid id, CancellationToken cancellationToken)
        => AsDocumentRecordConnection(await Get(id, cancellationToken).ConfigureAwait(false));

    private static IGenericResult<IDocumentRecordConnection> AsDocumentRecordConnection(IGenericResult<IGenericConnection> result)
    {
        if (result.IsFailure || result.Value is not { } connection)
        {
            return result.ToNewResult<IDocumentRecordConnection>();
        }

        return DocumentRecordConnections.Of(connection) is { } records
            ? GenericResult<IDocumentRecordConnection>.Success(records)
            : GenericResult<IDocumentRecordConnection>.Failure(
                ServicesResultCodes.ByName("ServiceCastFailed"),
                ResultDetails.Create("ExpectedType", nameof(IDocumentRecordConnection), "ActualType", connection.GetType().Name));
    }
}
