using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Opt-in interface for <see cref="IDataConnection"/> implementations whose store holds vectors, and so can answer a
/// vectorized command: the rows nearest a vector, say (FDW-1354).
/// </summary>
/// <remarks>
/// Every connection answers every data command; a vectorized command is not one, and only a connection implementing this
/// takes it. The DataGateway checks for this interface at runtime and refuses a vectorized command sent to a store whose
/// connection does not implement it, as it refuses a transaction on a connection that is not
/// <see cref="ITransactionalDataConnection"/>. A connection type that implements it also lists the <c>Vectorized</c>
/// command capability in its <c>SupportedCommands</c>, so configuration knows beforehand.
/// </remarks>
public interface IVectorizedDataConnection : IDataConnection
{
    /// <summary>
    /// Executes a vectorized command against the container.
    /// </summary>
    /// <typeparam name="T">What the command answers with.</typeparam>
    /// <param name="command">The vectorized command.</param>
    /// <param name="container">The container it runs against (provided by the DataGateway).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The command's answer, or why it failed.</returns>
    Task<IGenericResult<T>> Execute<T>(IVectorizedDataCommand<T> command, IDataContainer container, CancellationToken cancellationToken = default);
}
