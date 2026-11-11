using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Commands;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Commands;
using Fdw.Services.Data.Configuration;
using Fdw.Services.Data.Logging;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>Supplies the DataStore configuration.</summary>
public sealed class DataStoreConfigurationProvider
    : DomainConfigurationProviderBase<IDataStoreImplementationConfiguration, DataStoreDomainConfiguration>,
      IPlacedConfigurationProvider<IDataStoreImplementationConfiguration>,
      IDataStoreConfigurationProvider
{
    private readonly IConfigurationGatewayProvider _gateways;
    private readonly DataStoreTarget _target;
    private readonly IAuthenticationContextAccessor _accessor;
    private readonly ICrossTenantChangePolicy _crossTenant;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="DataStoreConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="placement">Places each member this provider makes, in the unit of work that makes it (FDW-3692).</param>
    /// <param name="accessor">The caller's authentication context, which a write of another tenant's data store is made under as the tenant it belongs to (FDW-4459).</param>
    /// <param name="crossTenant">Decides whether an administrator of another tenant may change the data store (FDW-4459).</param>
    public DataStoreConfigurationProvider(
        ILogger<DataStoreConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName,
        IConfigurationPlacement placement,
        IAuthenticationContextAccessor accessor,
        ICrossTenantChangePolicy crossTenant)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
        Placement = placement;
        _accessor = accessor;
        _crossTenant = crossTenant;
        _gateways = gatewayProvider;
        _target = new DataStoreTarget(dataStoreName, pathName, containerName);
        _logger = logger ?? NullLogger<DataStoreConfigurationProvider>.Instance;
    }

    /// <inheritdoc />
    public IConfigurationPlacement Placement { get; }

    /// <inheritdoc />
    public string PlacementKind => "DataStore";

    /// <inheritdoc />
    public bool Needs(IDataStoreImplementationConfiguration record) => true;

    /// <summary>
    /// Saves a new version of a data store that exists, as the tenant it belongs to once the cross-tenant change setting allows the caller's tenant to change it (FDW-4459).
    /// </summary>
    /// <typeparam name="T">The data store's implementation contract.</typeparam>
    /// <param name="store">The data store to save, carrying its durable Id.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success; a Forbidden failure that names the setting when the caller's tenant may not change it; another failure when the store or its tenant cannot be read or the write fails.</returns>
    public Task<IGenericResult> UpdateStore<T>(T store, CancellationToken cancellationToken = default)
        where T : IDataStoreImplementationConfiguration
        => WriteAsOwner(store.Id, () => Update(store, store.Domain, store.Implementation, store.Name, cancellationToken), cancellationToken);

    /// <summary>
    /// Deletes a data store, as the tenant it belongs to once the cross-tenant change setting allows the caller's tenant to change it (FDW-4459).
    /// </summary>
    /// <param name="id">The data store's durable Id.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success; a Forbidden failure that names the setting when the caller's tenant may not change it; another failure when the store or its tenant cannot be read or the delete fails.</returns>
    public Task<IGenericResult> DeleteStore(Guid id, CancellationToken cancellationToken = default)
        => WriteAsOwner(id, () => Delete(id, cancellationToken), cancellationToken);

    /// <summary>
    /// Answers whether the caller's tenant may change a data store that exists, without writing it (FDW-4459).
    /// </summary>
    /// <remarks>
    /// For a caller that does work before it writes (schema discovery opens the store's source first) and so should refuse before that work, not after it. The write verbs
    /// ask the same question again; nothing here is remembered.
    /// </remarks>
    /// <param name="storeId">The data store's durable Id.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>Success; a Forbidden failure that names the setting when the caller's tenant may not change it; another failure when the store or its tenant cannot be read.</returns>
    public async Task<IGenericResult> PermitWrite(Guid storeId, CancellationToken cancellationToken = default)
    {
        var owner = await ReadOwner(storeId, cancellationToken).ConfigureAwait(false);
        return owner.IsFailure
            ? owner
            : _crossTenant.Permit(CrossTenantChangeOption.DataStoreWrite, owner.Value!.TenantId, _accessor.Current?.ActiveTenantId);
    }

    // Why the policy is asked before the scope: the scope changes whose tenant is in force, and the question is about the caller's. The caller has been authorised by the
    // endpoint's policy and by reading the store (only one the caller can see); the scope decides only whose row the write belongs to, as when a platform administrator
    // changes a role of another tenant. A store that names no tenant cannot be read here, and no tenant is made up for it.
    private async Task<IGenericResult> WriteAsOwner(Guid storeId, Func<Task<IGenericResult>> write, CancellationToken cancellationToken)
    {
        var owner = await ReadOwner(storeId, cancellationToken).ConfigureAwait(false);
        if (owner.IsFailure)
        {
            return owner;
        }

        var ownerTenant = owner.Value!.TenantId;
        DataStoreCrossTenantLog.AskingPolicy(_logger, storeId, ownerTenant);
        var actorTenant = _accessor.Current?.ActiveTenantId;
        var permitted = _crossTenant.Permit(CrossTenantChangeOption.DataStoreWrite, ownerTenant, actorTenant);
        if (permitted.IsFailure)
        {
            return permitted;
        }

        IGenericResult written;
        if (actorTenant == ownerTenant)
        {
            written = await write().ConfigureAwait(false);
        }
        else
        {
            DataStoreCrossTenantLog.WrittenAsOwner(_logger, storeId, ownerTenant, actorTenant!.Value);
            using (new SystemAuthenticationContextScope(_accessor, ownerTenant))
            {
                written = await write().ConfigureAwait(false);
            }
        }

        // The policy has answered before the write, so a Forbidden failure here is the store's own row-level security: it answers our coded sentence and never the database's
        // text, as a connection's enable does (FDW-4193).
        return written.IsFailure && FailureCategory.IsForbidden(written)
            ? GenericResult.Failure(DataStoreCrossTenantLog.WriteRefusedByStore(_logger, storeId, actorTenant))
            : written;
    }

    // The part of the store's row that says whose it is: a store that is not there to this reader is a failure and never a stand-in tenant.
    private async Task<IGenericResult<DataStoreTenantConfiguration>> ReadOwner(Guid storeId, CancellationToken cancellationToken)
    {
        var gateway = _gateways.Get(DataStoreName);
        if (gateway.IsFailure)
        {
            return GenericResult<DataStoreTenantConfiguration>.Failure(DataStoreCrossTenantLog.OwnerNotRead(_logger, storeId, gateway.FailureReason().Message));
        }

        var command = new QueryCommand<DataStoreTenantConfiguration>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = "Id", Operator = FilterOperators.ByName("Equal"), Value = storeId },
                        new FilterCondition { PropertyName = "IsCurrent", Operator = FilterOperators.ByName("Equal"), Value = true },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        var rows = await gateway.Value!.Execute<IEnumerable<DataStoreTenantConfiguration>>(command, _target, useCache: false, cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure)
        {
            return GenericResult<DataStoreTenantConfiguration>.Failure(DataStoreCrossTenantLog.OwnerNotRead(_logger, storeId, rows.FailureReason().Message));
        }

        return (rows.Value ?? []).FirstOrDefault() is { } store
            ? GenericResult<DataStoreTenantConfiguration>.Success(store)
            : GenericResult<DataStoreTenantConfiguration>.Failure(DataStoreCrossTenantLog.StoreNotFound(_logger, storeId));
    }
}
