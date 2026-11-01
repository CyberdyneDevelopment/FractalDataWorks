using System.Collections.Generic;
using Fdw.Abstractions;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// A command only a store that holds vectors can answer: the rows nearest a vector, say. It is not a data command, which
/// every connection answers; a connection takes these only when it implements the vectorized capability, and the
/// DataGateway refuses one sent to a store whose connection does not (FDW-1354).
/// </summary>
public interface IVectorizedDataCommand : IGenericCommand
{
    /// <summary>
    /// Gets metadata for the command.
    /// </summary>
    /// <value>A read-only dictionary of metadata key-value pairs.</value>
    IReadOnlyDictionary<string, object> Metadata { get; }
}

/// <summary>
/// A vectorized command that answers with <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">What the command answers with.</typeparam>
public interface IVectorizedDataCommand<TResult> : IVectorizedDataCommand
{
    // Marker: the type parameter carries the result type, so a caller's Execute infers it.
}
