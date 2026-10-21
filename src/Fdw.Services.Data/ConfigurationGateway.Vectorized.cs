using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// The configuration gateway's vectorized entry (FDW-1354): a store the host declares answers a vectorized command when
/// its connection is vectorized, as it holds a transaction only when its connection is transactional.
/// </summary>
public sealed partial class ConfigurationGateway
{
    /// <inheritdoc/>
    public async Task<IGenericResult<T>> Execute<T>(IVectorizedDataCommand<T> command, DataStoreTarget target, CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveTarget(target, cancellationToken).ConfigureAwait(false);
        if (!resolved.IsSuccess)
            return resolved.ToNewResult<T>();

        var (container, connection) = resolved.Value;
        if (connection is not IVectorizedDataConnection vectorized)
            return GenericResult<T>.Failure(DataGatewayLogger.VectorizedNotSupported(_logger, target.DataStore, command.CommandType));

        return await RunOnContainer(target, ct => vectorized.Execute(command, container, ct), cancellationToken).ConfigureAwait(false);
    }
}
