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
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>Supplies the DataStore configuration.</summary>
/// <remarks>
/// A data store's <c>Description</c> is not a column of <c>data.DataStore</c> (FDW-6208): it is a row of the descriptions table under the description type
/// <c>DataStore</c>, written in the unit of the store's save, filled once for every set of composed stores and once for every page of domain records this provider
/// reads, and removed with the store. A write made as the store's own tenant (FDW-4459) writes the description as that tenant too, in that tenant's language.
/// The descriptions of its paths, containers, fields and keys are rows of the same table too (FDW-6589), each owned by the store: the store's save keeps them in
/// step, a read that composes the children fills them, a child read (a container's fields and keys) fills what it read, and they go with the store.
/// </remarks>
public sealed class DataStoreConfigurationProvider
    : DomainConfigurationProviderBase<IDataStoreImplementationConfiguration, DataStoreDomainConfiguration>,
      IPlacedConfigurationProvider<IDataStoreImplementationConfiguration>,
      IDataStoreConfigurationProvider,
      IDataStoreUncheckedWrites,
      IConfigurationWriteObserver<IDataStoreImplementationConfiguration>,
      IConfigurationReadObserver<IDataStoreImplementationConfiguration>,
      IConfigurationDomainReadObserver<DataStoreDomainConfiguration>,
      IConfigurationChildReadObserver<IDataStoreImplementationConfiguration>,
      IReferencedConfigurationProvider<IDataStoreImplementationConfiguration>
{
    private const string DescriptionType = DataStoreDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;
    private const string PathType = DataPathDescriptionType.OptionName;
    private const string ContainerType = DataContainerDescriptionType.OptionName;
    private const string FieldType = DataContainerFieldDescriptionType.OptionName;
    private const string KeyType = DataContainerKeyDescriptionType.OptionName;

    private readonly IConfigurationGatewayProvider _gateways;
    private readonly DataStoreTarget _target;
    private readonly IAuthenticationContextAccessor _accessor;
    private readonly ICrossTenantChangePolicy _crossTenant;
    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;
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
    /// <param name="writer">Writes each store's description in the unit of the store's save (FDW-6208).</param>
    /// <param name="reader">Reads the descriptions of the stores a read has just composed or paged, once for the set (FDW-6208).</param>
    public DataStoreConfigurationProvider(
        ILogger<DataStoreConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName,
        IConfigurationPlacement placement,
        IAuthenticationContextAccessor accessor,
        ICrossTenantChangePolicy crossTenant,
        IDescriptionWriter writer,
        IDescriptionReader reader)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
        Placement = placement;
        _accessor = accessor;
        _crossTenant = crossTenant;
        _writer = writer;
        _reader = reader;
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

    /// <inheritdoc />
    /// <remarks>
    /// A store saved with no text removes the one it had; nothing invents one. Its paths, containers, fields and keys are kept in step with one
    /// <see cref="IDescriptionWriter.DescribeOwned"/> for each type (FDW-6589): one read of what the store holds of it, a write only for what the save changed, and a
    /// removal for a child the store no longer holds or whose text is now empty. Every child carries its Id here: the save gives a new one its Id before this is called.
    /// </remarks>
    public async Task<IGenericResult> Written(IDataStoreImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        var own = await _writer.Describe(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken).ConfigureAwait(false);
        if (own.IsFailure) return own;

        var owner = new DescriptionOwner(record.Id, record.Name);
        var containers = record.Paths.SelectMany(path => path.Containers).ToList();
        foreach (var (type, things) in new (string Type, List<OwnedDescription> Things)[]
        {
            (PathType, [.. record.Paths.Select(path => new OwnedDescription(path.Id, path.Name, path.Description))]),
            (ContainerType, [.. containers.Select(container => new OwnedDescription(container.Id, container.Name, container.Description))]),
            (FieldType, [.. containers.SelectMany(container => container.Fields).Select(field => new OwnedDescription(field.Id, field.Name, field.Description))]),
            (KeyType, [.. containers.SelectMany(container => container.Keys).Select(key => new OwnedDescription(key.Id, key.Name, key.Description))]),
        })
        {
            var described = await _writer.DescribeOwned(type, DescriptionSubType, owner, things, unit, cancellationToken).ConfigureAwait(false);
            if (described.IsFailure) return described;
        }

        return GenericResult.Success();
    }

    /// <inheritdoc />
    /// <remarks>
    /// A store with no description row keeps an empty description; no other language's text and no column stands in for it. The children a read composed are filled
    /// with one read per child type for the whole set (FDW-6589); a read that composed no children (the record alone) costs none.
    /// </remarks>
    public async Task<IGenericResult> Read(IReadOnlyList<IDataStoreImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
    {
        var own = await _reader.Fill(DescriptionType, DescriptionSubType, records, store => store.Id, (store, text) => store.Description = text, cancellationToken).ConfigureAwait(false);
        if (own.IsFailure) return own;

        var paths = records.SelectMany(store => store.Paths).ToList();
        return paths.Count == 0
            ? GenericResult.Success()
            : await FillChildren([.. records.Select(store => store.Id)], paths, [], [], [], cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// A container's fields and keys are read as a child read (<c>Get&lt;DataContainerConfiguration, DataContainerFieldConfiguration&gt;</c>), which composes no store
    /// (FDW-6777): what it read is filled here from the descriptions the store owns, a path or a container with everything under it. Children of any other type are
    /// left as read.
    /// </remarks>
    public Task<IGenericResult> ReadChildren<TChild>(Guid memberId, IReadOnlyList<TChild> children, CancellationToken cancellationToken = default)
        => FillChildren(
            [memberId],
            [.. children.OfType<DataPathConfiguration>()],
            [.. children.OfType<DataContainerConfiguration>()],
            [.. children.OfType<DataContainerFieldConfiguration>()],
            [.. children.OfType<DataContainerKeyConfiguration>()],
            cancellationToken);

    /// <inheritdoc />
    /// <remarks>The data stores list reads pages of domain records, which the domain table answers without the text: it is filled here, once for the page.</remarks>
    public Task<IGenericResult> Read(IReadOnlyList<DataStoreDomainConfiguration> records, CancellationToken cancellationToken = default)
        => _reader.Fill(DescriptionType, DescriptionSubType, records, store => store.Id, (store, text) => store.Description = text, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A store's descriptions, and those of its paths, containers, fields and keys, name it, so they go with it, in every language, before it is retired.</remarks>
    public async Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        var own = await _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken).ConfigureAwait(false);
        if (own.IsFailure) return own;

        foreach (var type in new[] { PathType, ContainerType, FieldType, KeyType })
        {
            var removed = await _writer.DeleteOwnedBy(type, memberId, unit, cancellationToken).ConfigureAwait(false);
            if (removed.IsFailure) return removed;
        }

        return GenericResult.Success();
    }

    // Fills the paths, containers, fields and keys given, and everything under each path and container, from the descriptions the stores own: one read per child
    // type that has anything to fill, for the whole set.
    private async Task<IGenericResult> FillChildren(
        List<Guid> storeIds,
        List<DataPathConfiguration> paths,
        List<DataContainerConfiguration> containers,
        List<DataContainerFieldConfiguration> fields,
        List<DataContainerKeyConfiguration> keys,
        CancellationToken cancellationToken)
    {
        containers.AddRange(paths.SelectMany(path => path.Containers));
        fields.AddRange(containers.SelectMany(container => container.Fields));
        keys.AddRange(containers.SelectMany(container => container.Keys));

        var filled = await FillOwned(PathType, storeIds, paths, path => path.Id, (path, text) => path.Description = text, cancellationToken).ConfigureAwait(false);
        if (filled.IsFailure) return filled;

        filled = await FillOwned(ContainerType, storeIds, containers, container => container.Id, (container, text) => container.Description = text, cancellationToken).ConfigureAwait(false);
        if (filled.IsFailure) return filled;

        filled = await FillOwned(FieldType, storeIds, fields, field => field.Id, (field, text) => field.Description = text, cancellationToken).ConfigureAwait(false);
        return filled.IsFailure
            ? filled
            : await FillOwned(KeyType, storeIds, keys, key => key.Id, (key, text) => key.Description = text, cancellationToken).ConfigureAwait(false);
    }

    // One read of the descriptions of one child type the stores own; a child with no row keeps an empty description, and nothing stands in for it.
    private async Task<IGenericResult> FillOwned<T>(
        string type, List<Guid> storeIds, List<T> things, Func<T, Guid> idOf, Action<T, string> fill, CancellationToken cancellationToken)
    {
        if (things.Count == 0) return GenericResult.Success();

        var texts = await _reader.ReadOwnedBy(type, DescriptionSubType, storeIds, cancellationToken).ConfigureAwait(false);
        if (texts.IsFailure || texts.Value is not { } held) return texts;

        foreach (var thing in things)
        {
            if (held.TryGetValue(idOf(thing), out var text)) fill(thing, text.Text);
        }

        return GenericResult.Success();
    }

    /// <summary>
    /// Saves a new version of a data store that exists, as the tenant it belongs to once the cross-tenant change setting allows the caller's tenant to change it (FDW-4459).
    /// </summary>
    /// <typeparam name="T">The data store's implementation contract.</typeparam>
    /// <param name="store">The data store to save, carrying its durable Id.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success; a Forbidden failure that names the setting when the caller's tenant may not change it; another failure when the store or its tenant cannot be read or the write fails.</returns>
    /// <remarks>Unchecked as to who owns the store's paths and containers: only <see cref="DataStoreWrites"/> calls it, once it has decided (FDW-6968).</remarks>
    public Task<IGenericResult> UpdateStore<T>(T store, CancellationToken cancellationToken = default)
        where T : IDataStoreImplementationConfiguration
        => WriteAsOwner(store.Id, () => Update(store, store.Domain, store.Implementation, store.Name, cancellationToken), cancellationToken);

    /// <summary>
    /// Deletes a data store, as the tenant it belongs to once the cross-tenant change setting allows the caller's tenant to change it (FDW-4459).
    /// </summary>
    /// <param name="id">The data store's durable Id.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success; a Forbidden failure that names the setting when the caller's tenant may not change it; another failure when the store or its tenant cannot be read or the delete fails.</returns>
    /// <remarks>Unchecked as to who owns the store's paths and containers: only <see cref="DataStoreWrites"/> calls it, once it has decided (FDW-6968).</remarks>
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

    /// <inheritdoc />
    Task<IGenericResult<IDataStoreImplementationConfiguration>> IDataStoreUncheckedWrites.Stored(Guid storeId, CancellationToken cancellationToken)
        => Get(storeId, cancellationToken);

    /// <inheritdoc />
    Task<IGenericResult> IDataStoreUncheckedWrites.Update(IDataStoreImplementationConfiguration store, CancellationToken cancellationToken)
        => UpdateStore(store, cancellationToken);

    /// <inheritdoc />
    Task<IGenericResult> IDataStoreUncheckedWrites.Delete(Guid storeId, CancellationToken cancellationToken)
        => DeleteStore(storeId, cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> TenantOf(Guid storeId, CancellationToken cancellationToken = default)
    {
        var owner = await ReadOwner(storeId, cancellationToken).ConfigureAwait(false);
        return owner.IsFailure ? owner.ToNewResult<Guid>() : GenericResult<Guid>.Success(owner.Value!.TenantId);
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
