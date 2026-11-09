using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Configuration;

namespace Fdw.Services.Data;

/// <summary>Supplies the data stores: a store whole, a page of stores as domain records, and the writes that check whose store it is (FDW-4636).</summary>
/// <remarks>
/// Endpoints and services take this interface, never <see cref="DataStoreConfigurationProvider"/> (FDW-4618 rule 4). It sits beside the class because
/// <see cref="DataStoreDomainConfiguration"/>, the record a page of stores is read as, lives in this project.
/// </remarks>
public interface IDataStoreConfigurationProvider
    : IDomainConfigurationProvider<IDataStoreImplementationConfiguration>,
      IDomainRecordProvider<DataStoreDomainConfiguration>
{
    /// <summary>
    /// Saves a new version of a data store that exists, as the tenant it belongs to once the cross-tenant change setting allows the caller's tenant to change it (FDW-4459).
    /// </summary>
    /// <typeparam name="T">The data store's implementation contract.</typeparam>
    /// <param name="store">The data store to save, carrying its durable Id.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success; a Forbidden failure that names the setting when the caller's tenant may not change it; another failure when the store or its tenant cannot be read or the write fails.</returns>
    Task<IGenericResult> UpdateStore<T>(T store, CancellationToken cancellationToken = default)
        where T : IDataStoreImplementationConfiguration;

    /// <summary>
    /// Deletes a data store, as the tenant it belongs to once the cross-tenant change setting allows the caller's tenant to change it (FDW-4459).
    /// </summary>
    /// <param name="id">The data store's durable Id.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success; a Forbidden failure that names the setting when the caller's tenant may not change it; another failure when the store or its tenant cannot be read or the delete fails.</returns>
    Task<IGenericResult> DeleteStore(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers whether the caller's tenant may change a data store that exists, without writing it (FDW-4459).
    /// </summary>
    /// <param name="storeId">The data store's durable Id.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>Success; a Forbidden failure that names the setting when the caller's tenant may not change it; another failure when the store or its tenant cannot be read.</returns>
    Task<IGenericResult> PermitWrite(Guid storeId, CancellationToken cancellationToken = default);
}
