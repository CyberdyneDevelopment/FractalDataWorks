using Fdw.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections;
using Fdw.Services.Data.Logging;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Records and reads the owner of a path or a container of a shared data store, and decides whose scope a write of one is decided at (FDW-6967).
/// </summary>
/// <remarks>
/// <para>
/// The owner rows are read as the store's tenant, so who owns an item never depends on which workspaces the caller may see; the owning workspace's
/// name and organization are read the same way, so the item shows its owner to anyone who may see the item. A decision reads the owning workspace as
/// the caller: a caller who cannot see it cannot write it.
/// </para>
/// <para>
/// An owned item is decided at its owner's scope alone, with no platform-administrator override (FDW-6964, decision 3). An unowned one is decided by
/// the store's rule as before: <c>datastores:write</c>, then the cross-tenant check.
/// </para>
/// </remarks>
public sealed class DataStoreItemOwnership : IDataStoreItemOwnership
{
    private const string StoreResource = "datastores";
    private const string WriteAction = "write";

    private readonly IDataStoreConfigurationProvider _stores;
    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _access;
    private readonly IWorkspaceResourceOwners _owners;
    private readonly IFrameworkAuthorizationService _authorization;
    private readonly IAuthenticationContextAccessor _accessor;
    private readonly ILogger<DataStoreItemOwnership> _logger;

    /// <summary>Initializes a new instance of the <see cref="DataStoreItemOwnership"/> class.</summary>
    /// <param name="stores">Reads the store, its tenant, and asks the store's cross-tenant check.</param>
    /// <param name="workspaces">Reads the owning and the named workspace.</param>
    /// <param name="access">Decides whether the caller may change a workspace: the owner's scope.</param>
    /// <param name="owners">Reads and writes the <c>Owns</c> rows.</param>
    /// <param name="authorization">Answers whether the caller holds the store's write permission.</param>
    /// <param name="accessor">The caller's authentication context.</param>
    /// <param name="logger">The logger.</param>
    public DataStoreItemOwnership(
        IDataStoreConfigurationProvider stores,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IWorkspaceResourceOwners owners,
        IFrameworkAuthorizationService authorization,
        IAuthenticationContextAccessor accessor,
        ILogger<DataStoreItemOwnership>? logger = null)
    {
        _stores = stores;
        _workspaces = workspaces;
        _access = access;
        _owners = owners;
        _authorization = authorization;
        _accessor = accessor;
        _logger = logger ?? NullLogger<DataStoreItemOwnership>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<DataStoreItemOwners>> Owners(Guid storeId, CancellationToken cancellationToken = default)
    {
        var tenant = await _stores.TenantOf(storeId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure) return tenant.ToNewResult<DataStoreItemOwners>();

        var paths = await OwnersOf(DataPathResourceKind.KindName, tenant.Value, cancellationToken).ConfigureAwait(false);
        if (paths.IsFailure) return paths.ToNewResult<DataStoreItemOwners>();

        var containers = await OwnersOf(DataContainerResourceKind.KindName, tenant.Value, cancellationToken).ConfigureAwait(false);
        if (containers.IsFailure) return containers.ToNewResult<DataStoreItemOwners>();

        return GenericResult<DataStoreItemOwners>.Success(new DataStoreItemOwners(tenant.Value, paths.Value!, containers.Value!));
    }

    /// <inheritdoc />
    public async Task<IGenericResult> MayWrite(Guid storeId, DataStoreItemAddress item, DataStoreItemOwner? owner, CancellationToken cancellationToken = default)
    {
        if (_accessor.Current is not { IsAuthenticated: true } caller)
            return GenericResult.Failure(DataStoreItemOwnershipLog.CallerUnresolved(_logger, item.ToString()));

        if (owner is not null)
        {
            DataStoreItemOwnershipLog.WriteDecidedAt(_logger, item.ToString(), string.Concat("workspace '", owner.WorkspaceName, "'"));
            var workspace = await _workspaces.Get(owner.WorkspaceId, ConfigurationLevels.WholeAggregate, ConfigurationReadOptions.WithoutFilledMembers, cancellationToken).ConfigureAwait(false);
            if (workspace.IsFailure) return workspace;
            if (workspace.Value is null)
                return GenericResult.Failure(DataStoreItemOwnershipLog.WorkspaceNotFound(_logger, owner.WorkspaceName));

            return await _access.MayWrite(workspace.Value, cancellationToken).ConfigureAwait(false);
        }

        DataStoreItemOwnershipLog.WriteDecidedAt(_logger, item.ToString(), "its data store");
        var permitted = await _authorization.Authorize(caller, StoreResource, WriteAction, cancellationToken).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted;
        if (!permitted.Value)
            return GenericResult.Failure(DataStoreItemOwnershipLog.StorePermissionMissing(_logger, item.ToString(), string.Concat(StoreResource, ":", WriteAction)));

        return await _stores.PermitWrite(storeId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<DataStoreItemOwner>> Assign(DataStoreItemAddress item, string workspaceName, CancellationToken cancellationToken = default)
    {
        var found = await Find(item, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure) return found.ToNewResult<DataStoreItemOwner>();
        var (store, path, container, owners) = found.Value;

        var workspace = await _workspaces.Get(workspaceName, cancellationToken).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace.ToNewResult<DataStoreItemOwner>();
        if (workspace.Value is null)
            return GenericResult<DataStoreItemOwner>.Failure(DataStoreItemOwnershipLog.WorkspaceNotFound(_logger, workspaceName));

        // The same owner again is the state asked for; another owner must release it first, so one item never has two.
        if (owners.OwnOf(path, container) is { } current)
        {
            return current.WorkspaceId == workspace.Value.Id
                ? GenericResult<DataStoreItemOwner>.Success(current)
                : GenericResult<DataStoreItemOwner>.Failure(DataStoreItemOwnershipLog.OwnedByAnother(_logger, item.ToString(), current.WorkspaceName));
        }

        if (workspace.Value.TenantId != owners.TenantId)
            return GenericResult<DataStoreItemOwner>.Failure(DataStoreItemOwnershipLog.WorkspaceNotInStoreTenant(_logger, item.ToString(), workspaceName));

        // Lending the item is a change of it, decided where its writes are decided now: its path's owner for a container under an owned path, else the store.
        var mayChangeItem = await MayWrite(store.Id, item, owners.Of(path, container), cancellationToken).ConfigureAwait(false);
        if (mayChangeItem.IsFailure) return mayChangeItem.ToNewResult<DataStoreItemOwner>();

        var mayChangeWorkspace = await _access.MayWrite(workspace.Value, cancellationToken).ConfigureAwait(false);
        if (mayChangeWorkspace.IsFailure) return mayChangeWorkspace.ToNewResult<DataStoreItemOwner>();

        var owned = container is null
            ? await _owners.Own(workspace.Value, DataPathResourceKind.KindName, path.Id, path.Name, cancellationToken).ConfigureAwait(false)
            : await _owners.Own(workspace.Value, DataContainerResourceKind.KindName, container.Id, container.Name, cancellationToken).ConfigureAwait(false);
        if (owned.IsFailure) return owned.ToNewResult<DataStoreItemOwner>();

        DataStoreItemOwnershipLog.OwnerAssigned(_logger, item.ToString(), workspace.Value.Name);
        return GenericResult<DataStoreItemOwner>.Success(
            new DataStoreItemOwner(workspace.Value.Id, workspace.Value.Name, workspace.Value.OrganizationId, owned.Value!.Id, ThroughPath: false));
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Release(DataStoreItemAddress item, CancellationToken cancellationToken = default)
    {
        var found = await Find(item, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure) return found;
        var (store, path, container, owners) = found.Value;

        if (owners.OwnOf(path, container) is not { } owner)
            return GenericResult.Failure(DataStoreItemOwnershipLog.NotOwned(_logger, item.ToString()));

        // Decided at the owner's scope alone (FDW-6964, decision 3): the store's administrators release an owned item only as members of its owner.
        var permitted = await MayWrite(store.Id, item, owner, cancellationToken).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted;

        var released = await _owners.Release(
            new WorkspaceResourceOwner(owner.AttachmentId, container is null ? path.Id : container.Id, owner.WorkspaceId), cancellationToken).ConfigureAwait(false);
        if (released.IsFailure) return released;

        DataStoreItemOwnershipLog.OwnerReleased(_logger, item.ToString(), owner.WorkspaceName);
        return GenericResult.Success();
    }

    // The store, path and container an address names, and who owns the store's items.
    private async Task<IGenericResult<(IDataStoreImplementationConfiguration Store, DataPathConfiguration Path, DataContainerConfiguration? Container, DataStoreItemOwners Owners)>> Find(
        DataStoreItemAddress item, CancellationToken cancellationToken)
    {
        var store = await _stores.Get(item.StoreName, ConfigurationLevels.WholeAggregate, ConfigurationReadOptions.WithoutFilledMembers, cancellationToken).ConfigureAwait(false);
        if (store.IsFailure || store.Value is null)
            return GenericResult<(IDataStoreImplementationConfiguration, DataPathConfiguration, DataContainerConfiguration?, DataStoreItemOwners)>.Failure(
                DataStoreItemOwnershipLog.StoreNotFound(_logger, item.StoreName));

        var path = store.Value.Paths.FirstOrDefault(p => string.Equals(p.Name, item.PathName, StringComparison.OrdinalIgnoreCase));
        if (path is null)
            return GenericResult<(IDataStoreImplementationConfiguration, DataPathConfiguration, DataContainerConfiguration?, DataStoreItemOwners)>.Failure(
                DataStoreItemOwnershipLog.PathNotFound(_logger, item.StoreName, item.PathName));

        DataContainerConfiguration? container = null;
        if (item.ContainerName is { } containerName)
        {
            container = path.Containers.FirstOrDefault(c => string.Equals(c.Name, containerName, StringComparison.OrdinalIgnoreCase));
            if (container is null)
                return GenericResult<(IDataStoreImplementationConfiguration, DataPathConfiguration, DataContainerConfiguration?, DataStoreItemOwners)>.Failure(
                    DataStoreItemOwnershipLog.ContainerNotFound(_logger, item.StoreName, item.PathName, containerName));
        }

        var owners = await Owners(store.Value.Id, cancellationToken).ConfigureAwait(false);
        return owners.IsFailure
            ? owners.ToNewResult<(IDataStoreImplementationConfiguration, DataPathConfiguration, DataContainerConfiguration?, DataStoreItemOwners)>()
            : GenericResult<(IDataStoreImplementationConfiguration, DataPathConfiguration, DataContainerConfiguration?, DataStoreItemOwners)>.Success(
                (store.Value, path, container, owners.Value!));
    }

    // The owners of one kind in the store's tenant, by the owned item's Id, each with its workspace's name and organization. Two rows for one item
    // fail loud: one owner is the rule, and none is picked.
    private async Task<IGenericResult<IReadOnlyDictionary<Guid, DataStoreItemOwner>>> OwnersOf(string kind, Guid tenantId, CancellationToken cancellationToken)
    {
        var rows = await _owners.Owned(kind, tenantId, cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure) return rows.ToNewResult<IReadOnlyDictionary<Guid, DataStoreItemOwner>>();

        var owners = new Dictionary<Guid, DataStoreItemOwner>();
        var workspaces = new Dictionary<Guid, IWorkspaceImplementationConfiguration>();
        foreach (var group in rows.Value!.GroupBy(row => row.ResourceId))
        {
            var held = group.ToList();
            var resourceText = string.Concat(kind, " ", group.Key.ToString("D", CultureInfo.InvariantCulture));
            if (held.Count > 1)
                return GenericResult<IReadOnlyDictionary<Guid, DataStoreItemOwner>>.Failure(DataStoreItemOwnershipLog.OwnerAmbiguous(_logger, resourceText, held.Count));

            var workspace = await Workspace(held[0].WorkspaceId, tenantId, resourceText, workspaces, cancellationToken).ConfigureAwait(false);
            if (workspace.IsFailure) return workspace.ToNewResult<IReadOnlyDictionary<Guid, DataStoreItemOwner>>();

            owners[group.Key] = new DataStoreItemOwner(workspace.Value!.Id, workspace.Value.Name, workspace.Value.OrganizationId, held[0].AttachmentId, ThroughPath: false);
        }

        return GenericResult<IReadOnlyDictionary<Guid, DataStoreItemOwner>>.Success(owners);
    }

    // The owning workspace, read as the store's tenant (so the item names its owner to anyone who sees the item), once per workspace.
    private async Task<IGenericResult<IWorkspaceImplementationConfiguration>> Workspace(
        Guid workspaceId, Guid tenantId, string resourceText, Dictionary<Guid, IWorkspaceImplementationConfiguration> read, CancellationToken cancellationToken)
    {
        if (read.TryGetValue(workspaceId, out var known))
            return GenericResult<IWorkspaceImplementationConfiguration>.Success(known);

        IGenericResult<IWorkspaceImplementationConfiguration> workspace;
        using (new SystemAuthenticationContextScope(_accessor, tenantId))
        {
            workspace = await _workspaces.Get(workspaceId, ConfigurationLevels.WholeAggregate, ConfigurationReadOptions.WithoutFilledMembers, cancellationToken).ConfigureAwait(false);
        }

        if (workspace.IsFailure || workspace.Value is null)
            return GenericResult<IWorkspaceImplementationConfiguration>.Failure(
                DataStoreItemOwnershipLog.OwnerWorkspaceMissing(_logger, resourceText, workspaceId.ToString("D", CultureInfo.InvariantCulture)));

        read[workspaceId] = workspace.Value;
        return workspace;
    }
}
