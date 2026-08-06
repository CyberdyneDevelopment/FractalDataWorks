using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Opt-in interface for <see cref="IDataConnection"/> implementations that support
/// starting a database transaction.
/// </summary>
/// <remarks>
/// Not all connection types support transactions (e.g., REST connections do not).
/// The DataGateway BeginTransaction method checks for this interface at runtime.
/// Connection types that do not implement it return a failure from BeginTransaction.
/// </remarks>
public interface ITransactionalDataConnection : IDataConnection
{
    /// <summary>
    /// Starts a transaction on this connection and returns a scope that can execute
    /// commands inside it.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the open operation.</param>
    /// <returns>
    /// A result containing the transaction scope on success, or a structured failure if the
    /// underlying connection cannot be opened or the transaction cannot be started.
    /// </returns>
    Task<IGenericResult<IDataConnectionTransaction>> BeginTransaction(
        CancellationToken cancellationToken = default);
}
