using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Translates a vectorized command into a vectorized connection's own command (a SqlCommand, say).
/// </summary>
/// <typeparam name="TCommand">The connection's command type.</typeparam>
public interface IVectorizedDataCommandTranslator<TCommand> : ITypeOption<int>
{
    /// <summary>
    /// Translates <paramref name="command"/> for <paramref name="container"/>.
    /// </summary>
    /// <param name="command">The vectorized command.</param>
    /// <param name="container">The container it runs against, with its schema.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The connection's command, or why it could not be translated.</returns>
    Task<IGenericResult<TCommand>> Translate(
        IVectorizedDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default);
}
