using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Configuration;

namespace Fdw.Services.Data;

/// <summary>Supplies the data stores: a store whole, a page of stores as domain records, and whether the caller's tenant may change one (FDW-4636).</summary>
/// <remarks>
/// Endpoints and services take this interface, never <see cref="DataStoreConfigurationProvider"/> (FDW-4618 rule 4). It sits beside the class because
/// <see cref="DataStoreDomainConfiguration"/>, the record a page of stores is read as, lives in this project.
/// A store that exists is changed or deleted through <see cref="IDataStoreWrites"/>, which decides each path and container it touches at its
/// owner's scope or the store's (FDW-6968); this interface does not offer those writes, so nothing can make them unchecked.
/// </remarks>
public interface IDataStoreConfigurationProvider
    : IDomainConfigurationProvider<IDataStoreImplementationConfiguration>,
      IDomainRecordProvider<DataStoreDomainConfiguration>
{
    /// <summary>
    /// Answers whether the caller's tenant may change a data store that exists, without writing it (FDW-4459).
    /// </summary>
    /// <param name="storeId">The data store's durable Id.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>Success; a Forbidden failure that names the setting when the caller's tenant may not change it; another failure when the store or its tenant cannot be read.</returns>
    Task<IGenericResult> PermitWrite(Guid storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the tenant a data store belongs to (FDW-6967).
    /// </summary>
    /// <param name="storeId">The data store's durable Id.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The tenant; a failure when the store or its tenant cannot be read.</returns>
    /// <remarks>Who owns a path or a container of the store is read as this tenant, so the answer does not depend on who asks.</remarks>
    Task<IGenericResult<Guid>> TenantOf(Guid storeId, CancellationToken cancellationToken = default);
}
