using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// The DataGateway's vectorized entry (FDW-1354): a vectorized command runs only on a store whose connection is
/// vectorized, under that connection's limits, and is never cached.
/// </summary>
public sealed partial class DataGatewayService
{
    /// <inheritdoc/>
    /// <remarks>
    /// A target in a store the host declared is not run here, as for a data command: a non-system caller is refused, and
    /// the system context is sent to the store's configuration gateway (FDW-1068).
    /// </remarks>
    public async Task<IGenericResult<T>> Execute<T>(IVectorizedDataCommand<T> command, DataStoreTarget target, CancellationToken cancellationToken = default)
    {
        if (await IsHostDeclared(target.DataStore, cancellationToken).ConfigureAwait(false))
        {
            var gateway = HostStoreGateway(target.DataStore, target.Container);
            if (gateway.IsFailure || gateway.Value is not { } hostGateway)
                return gateway.ToNewResult<T>();

            return await hostGateway.Execute(command, target, cancellationToken).ConfigureAwait(false);
        }

        return await ExecuteAdmitted(command, target.DataStore, ct => ExecuteVectorized(command, target, ct), cancellationToken).ConfigureAwait(false);
    }

    // As BeginTransaction checks for ITransactionalDataConnection: the store's connection either holds vectors or the
    // command is refused, naming the store.
    private async Task<IGenericResult<T>> ExecuteVectorized<T>(IVectorizedDataCommand<T> command, DataStoreTarget target, CancellationToken cancellationToken)
    {
        var resolved = await ResolveTargetConnection(target, cancellationToken).ConfigureAwait(false);
        if (!resolved.IsSuccess)
            return resolved.ToNewResult<T>();

        var (container, connection) = resolved.Value;
        if (connection is not IVectorizedDataConnection vectorized)
            return GenericResult<T>.Failure(DataGatewayLogger.VectorizedNotSupported(_logger, target.DataStore, command.CommandType));

        return await RunOnContainer(command, target, ct => vectorized.Execute(command, container, ct), cancellationToken).ConfigureAwait(false);
    }
}
