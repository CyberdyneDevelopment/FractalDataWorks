using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections;

namespace Fdw.Services.Data;

/// <summary>
/// The data store writes <see cref="DataStoreWrites"/> makes once it has decided them (FDW-6968): unchecked as to who owns the store's paths and
/// containers, so internal to the data package and never offered to a caller.
/// </summary>
internal interface IDataStoreUncheckedWrites
{
    /// <summary>Reads a store whole as it is stored now.</summary>
    /// <param name="storeId">The store's durable Id.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The store; null when it is not there; a failure when it cannot be read.</returns>
    Task<IGenericResult<IDataStoreImplementationConfiguration>> Stored(Guid storeId, CancellationToken cancellationToken);

    /// <summary>Saves a new version of a store that exists, with the cross-tenant check.</summary>
    /// <param name="store">The store to save.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success, or why it was not saved.</returns>
    Task<IGenericResult> Update(IDataStoreImplementationConfiguration store, CancellationToken cancellationToken);

    /// <summary>Deletes a store, with the cross-tenant check.</summary>
    /// <param name="storeId">The store's durable Id.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success, or why it was not deleted.</returns>
    Task<IGenericResult> Delete(Guid storeId, CancellationToken cancellationToken);
}
