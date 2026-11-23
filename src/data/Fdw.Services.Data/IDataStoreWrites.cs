using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections;

namespace Fdw.Services.Data;

/// <summary>
/// The writes of a data store that exists, each decided item by item: a path or container a workspace owns at its owner's scope alone, the rest by
/// the store's rule (FDW-6968).
/// </summary>
/// <remarks>
/// The one way to change or delete a data store that exists. <see cref="IDataStoreConfigurationProvider"/> no longer offers its unchecked
/// <c>UpdateStore</c> and <c>DeleteStore</c>, so a new write path cannot skip the decision: it has nothing else to call.
/// </remarks>
public interface IDataStoreWrites
{
    /// <summary>Saves a new version of a data store once every path and container it changes may be written by the caller.</summary>
    /// <param name="store">The store as it should be saved, carrying its durable Id.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success; the first refusal, naming the owner or the store's permission; a failure when the store cannot be read, compared or written.</returns>
    Task<IGenericResult> UpdateStore(IDataStoreImplementationConfiguration store, CancellationToken cancellationToken = default);

    /// <summary>Deletes a data store once the caller may write every path and container it holds.</summary>
    /// <param name="storeId">The store's durable Id.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success; the first refusal, naming the owner or the store's permission; a failure when the store cannot be read or deleted.</returns>
    Task<IGenericResult> DeleteStore(Guid storeId, CancellationToken cancellationToken = default);
}
