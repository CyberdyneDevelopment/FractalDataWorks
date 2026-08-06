using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// Default implementation of <see cref="IDataGatewayTransaction"/>.
/// Wraps an <see cref="IDataConnectionTransaction"/> opened by a transactional connection.
/// Container resolution uses the same DataStore tree path as <see cref="DataGatewayService"/>.
/// </summary>
internal sealed class DataGatewayTransaction : IDataGatewayTransaction
{
    private readonly IDataConnectionTransaction _connectionTransaction;
    private readonly string _connectionName;
    // Why: delegate signature changed from Func<IDataCommand,…> to Func<DataStoreTarget,…>
    // in the target-typed-gateway refactor. Addressing now lives in DataStoreTarget, not commands.
    private readonly Func<DataStoreTarget, CancellationToken, Task<IGenericResult<IDataContainer>>> _resolveContainer;
    private readonly ILogger _logger;
    // Why: the cross-connection guard prevents a command bound to a different connection from
    // silently running outside this transaction's physical scope. It only makes sense for a
    // multi-connection gateway. ConfigurationGateway is single-connection (every command is
    // routed to its one ConfigurationDb connection regardless of the command's ConnectionName),
    // so it opts out — otherwise a command whose ConnectionName defaults to "Default" is wrongly
    // rejected against the transaction's data-store name.
    private readonly bool _enforceConnectionMatch;
    private bool _disposed;

    internal DataGatewayTransaction(
        IDataConnectionTransaction connectionTransaction,
        string connectionName,
        Func<DataStoreTarget, CancellationToken, Task<IGenericResult<IDataContainer>>> resolveContainer,
        ILogger logger,
        bool enforceConnectionMatch = true)
    {
        _connectionTransaction = connectionTransaction;
        _connectionName = connectionName;
        _resolveContainer = resolveContainer;
        _logger = logger;
        _enforceConnectionMatch = enforceConnectionMatch;
    }

    /// <inheritdoc />
    public bool IsActive => !_disposed && _connectionTransaction.IsActive;

    /// <inheritdoc />
    public async Task<IGenericResult<T>> Execute<T>(
        IDataCommand command,
        DataStoreTarget target,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return GenericResult<T>.Failure(DataGatewayLogger.BeginTransactionFailed(_logger, _connectionName, "Transaction scope is disposed."));

        // Why: Guard against cross-connection writes that would silently execute outside
        // the transaction's physical scope (they would use a different SqlConnection).
        // target.DataStore identifies the logical store; compare against _connectionName.
        // Skipped for single-connection gateways (see _enforceConnectionMatch).
        if (_enforceConnectionMatch &&
            !string.Equals(target.DataStore, _connectionName, StringComparison.OrdinalIgnoreCase))
        {
            return GenericResult<T>.Failure(
                DataGatewayLogger.TransactionConnectionMismatch(_logger, _connectionName, target.DataStore));
        }

        // Why (FDW-583): propagate the resolver's own failure message — which names the missing
        // store/path/container or the underlying DB error — instead of collapsing it to null and
        // re-deriving a generic "could not be resolved" that hides which cause actually fired.
        var containerResult = await _resolveContainer(target, cancellationToken).ConfigureAwait(false);
        if (!containerResult.IsSuccess)
            return containerResult.ToNewResult<T>();
        if (containerResult.Value is null)
        {
            return GenericResult<T>.Failure(
                DataGatewayLogger.BeginTransactionFailed(_logger, _connectionName, $"Container '{target.Container}' could not be resolved."));
        }

        return await _connectionTransaction.Execute<T>(command, containerResult.Value, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Execute(
        IDataCommand command,
        DataStoreTarget target,
        CancellationToken cancellationToken = default)
    {
        var result = await Execute<object>(command, target, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? GenericResult.Success() : result;
    }

    /// <inheritdoc />
    public Task<IGenericResult> Commit(CancellationToken cancellationToken = default)
        => _connectionTransaction.Commit(cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult> Rollback(CancellationToken cancellationToken = default)
        => _connectionTransaction.Rollback(cancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await _connectionTransaction.DisposeAsync().ConfigureAwait(false);
    }
}
