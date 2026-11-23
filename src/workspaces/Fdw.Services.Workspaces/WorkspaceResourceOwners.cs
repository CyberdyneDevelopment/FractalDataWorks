using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces.Abstractions;
using Fdw.Services.Workspaces.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Reads and writes the <c>Owns</c> rows of <c>workspace.WorkspaceResource</c> (FDW-6967).
/// </summary>
/// <remarks>
/// <para>
/// Owners are read from the resource side, by kind and Id, as the resource's tenant: who owns a thing must not depend on which workspaces the
/// asker may see, or a resource owned by a workspace hidden from the asker would read as owned by nobody. Only the read is raised; writes run as
/// the caller, who has been asked through <see cref="IWorkspaceAccessPolicy"/> before this is called.
/// </para>
/// <para>
/// A new owner is written as a child of the workspace through its provider, the way a resource is attached. A release retires the one row by its
/// Id: the workspace's resource list is upsert-only, so a save that leaves a row out does not retire it.
/// </para>
/// </remarks>
public sealed class WorkspaceResourceOwners : IWorkspaceResourceOwners
{
    private const string ResourceContainer = "WorkspaceResource";

    private readonly IConfigurationGatewayProvider _gateways;
    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly IAuthenticationContextAccessor _accessor;
    private readonly DataStoreTarget _target;
    private readonly string _pathName;
    private readonly ILogger<WorkspaceResourceOwners> _logger;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceResourceOwners"/> class.</summary>
    /// <param name="gateways">Supplies the configuration gateway the workspace rows live on.</param>
    /// <param name="workspaces">Saves a workspace with a new resource under it.</param>
    /// <param name="accessor">The caller's authentication context: who records an owner, and the context a read of owners is raised from.</param>
    /// <param name="dataStoreName">The configuration connection the workspace rows live on.</param>
    /// <param name="pathName">The schema the workspace rows live in.</param>
    /// <param name="logger">The logger.</param>
    public WorkspaceResourceOwners(
        IConfigurationGatewayProvider gateways,
        IWorkspaceConfigurationProvider workspaces,
        IAuthenticationContextAccessor accessor,
        string dataStoreName,
        string pathName,
        ILogger<WorkspaceResourceOwners>? logger = null)
    {
        _gateways = gateways;
        _workspaces = workspaces;
        _accessor = accessor;
        // The path is held as the string it was registered with: DataStoreTarget's Path is nullable (a target that names a store and a container only), and
        // the workspace rows always sit under a path, so a missing one is refused where it is used (WORKSPACES-61760), never stood in for.
        _pathName = pathName;
        _target = new DataStoreTarget(dataStoreName, pathName, ResourceContainer);
        _logger = logger ?? NullLogger<WorkspaceResourceOwners>.Instance;
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<WorkspaceResourceOwner>>> Owners(
        string kind, Guid resourceId, Guid tenantId, CancellationToken cancellationToken = default)
        => Read(kind, resourceId.ToString("D", CultureInfo.InvariantCulture), OwnsRows(kind).Where(nameof(WorkspaceResourceOwnerRow.ResourceId), resourceId), tenantId, cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<WorkspaceResourceOwner>>> Owned(string kind, Guid tenantId, CancellationToken cancellationToken = default)
        => Read(kind, string.Concat("every ", kind), OwnsRows(kind), tenantId, cancellationToken);

    // The current Owns rows of one kind; a caller narrows it further.
    private QueryCommandBuilder<WorkspaceResourceOwnerRow> OwnsRows(string kind) =>
        new QueryCommandBuilder<WorkspaceResourceOwnerRow>(_target.DataStore, _pathName, ResourceContainer)
            .Where(nameof(WorkspaceResourceOwnerRow.ResourceType), kind)
            .Where(nameof(WorkspaceResourceOwnerRow.Relationship), WorkspaceResourceRelationships.ByName("Owns").Name)
            .Where(nameof(WorkspaceResourceOwnerRow.IsCurrent), true)
            .Where(nameof(WorkspaceResourceOwnerRow.IsDeleted), false);

    // Reads the rows as the tenant they belong to: who owns a thing must not depend on which workspaces the asker may see.
    private async Task<IGenericResult<IReadOnlyList<WorkspaceResourceOwner>>> Read(
        string kind, string described, QueryCommandBuilder<WorkspaceResourceOwnerRow> query, Guid tenantId, CancellationToken cancellationToken)
    {
        if (_pathName.Length == 0)
            return GenericResult<IReadOnlyList<WorkspaceResourceOwner>>.Failure(WorkspaceResourceOwnersLog.PathNotDeclared(_logger, _target.DataStore, ResourceContainer));

        var gateway = _gateways.Get(_target.DataStore);
        if (gateway.IsFailure)
        {
            return GenericResult<IReadOnlyList<WorkspaceResourceOwner>>.Failure(
                WorkspaceResourceOwnersLog.OwnersNotRead(_logger, kind, described, gateway.FailureReason().Message));
        }

        IGenericResult<IEnumerable<WorkspaceResourceOwnerRow>> rows;
        using (new SystemAuthenticationContextScope(_accessor, tenantId))
        {
            rows = await gateway.Value!.Execute<IEnumerable<WorkspaceResourceOwnerRow>>(query.Build().Command, _target, useCache: false, cancellationToken).ConfigureAwait(false);
        }

        if (rows.IsFailure)
        {
            return GenericResult<IReadOnlyList<WorkspaceResourceOwner>>.Failure(
                WorkspaceResourceOwnersLog.OwnersNotRead(_logger, kind, described, rows.FailureReason().Message));
        }

        List<WorkspaceResourceOwner> owners = [.. (rows.Value ?? []).Select(row => new WorkspaceResourceOwner(row.Id, row.ResourceId, row.WorkspaceConfigurationId))];
        WorkspaceResourceOwnersLog.OwnersRead(_logger, kind, described, owners.Count);
        return GenericResult<IReadOnlyList<WorkspaceResourceOwner>>.Success(owners);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<WorkspaceResourceConfiguration>> Own(
        IWorkspaceImplementationConfiguration workspace, string kind, Guid resourceId, string label, CancellationToken cancellationToken = default)
    {
        var declared = WorkspaceResourceKinds.ByName(kind);
        if (ReferenceEquals(declared, WorkspaceResourceKinds.NotFound))
            return GenericResult<WorkspaceResourceConfiguration>.Failure(WorkspaceResourceOwnersLog.KindUnknown(_logger, workspace.Name, kind));

        if (!declared.CanBeOwned)
            return GenericResult<WorkspaceResourceConfiguration>.Failure(WorkspaceResourceOwnersLog.KindNotOwnable(_logger, workspace.Name, kind));

        if (_accessor.Current is not { } caller || !Guid.TryParse(caller.UserId, out var addedByUserId))
            return GenericResult<WorkspaceResourceConfiguration>.Failure(WorkspaceResourceOwnersLog.CallerUnresolved(_logger, workspace.Name));

        var resource = new WorkspaceResourceConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = label,
            ResourceType = declared.Name,
            ResourceId = resourceId,
            Relationship = WorkspaceResourceRelationships.ByName("Owns").Name,
            AddedByUserId = addedByUserId,
            // Stamped for the reason the attach endpoint stamps it: the insert writes every mapped column, so an unset value would overwrite the
            // column default (FDW-793).
            CreateDate = DateTimeOffset.UtcNow,
        };

        // A child of the workspace aggregate, saved with it as an attachment is; the list is upsert-only, so the rows already there are kept.
        workspace.Resources.Add(resource);
        var saved = await _workspaces.Update(workspace, workspace.Domain, workspace.Implementation, workspace.Name, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            workspace.Resources.Remove(resource);
            return saved.ToNewResult<WorkspaceResourceConfiguration>();
        }

        WorkspaceResourceOwnersLog.Owned(_logger, workspace.Name, declared.Name, resourceId.ToString("D", CultureInfo.InvariantCulture));
        return GenericResult<WorkspaceResourceConfiguration>.Success(resource);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Release(WorkspaceResourceOwner owner, CancellationToken cancellationToken = default)
    {
        var attachmentText = owner.AttachmentId.ToString("D", CultureInfo.InvariantCulture);
        if (_pathName.Length == 0)
            return GenericResult.Failure(WorkspaceResourceOwnersLog.PathNotDeclared(_logger, _target.DataStore, ResourceContainer));

        var gateway = _gateways.Get(_target.DataStore);
        if (gateway.IsFailure)
            return GenericResult.Failure(WorkspaceResourceOwnersLog.ReleaseFailed(_logger, attachmentText, gateway.FailureReason().Message));

        var retired = await gateway.Value!.Execute<ConfigurationWriteResult>(
            new ConfigurationRetireCommand((KeyTypeBase)KeyTypes.ByName("Logical"), [owner.AttachmentId], markDeleted: true),
            _target, cancellationToken).ConfigureAwait(false);
        if (retired.IsFailure)
            return GenericResult.Failure(WorkspaceResourceOwnersLog.ReleaseFailed(_logger, attachmentText, retired.FailureReason().Message));

        WorkspaceResourceOwnersLog.Released(_logger, owner.WorkspaceId.ToString("D", CultureInfo.InvariantCulture), attachmentText);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<WorkspaceResourceConfiguration>>> ReleaseOnArchive(
        IWorkspaceImplementationConfiguration workspace, CancellationToken cancellationToken = default)
    {
        var owns = WorkspaceResourceRelationships.ByName("Owns").Name;
        List<WorkspaceResourceConfiguration> released = [.. workspace.Resources.Where(resource =>
            string.Equals(resource.Relationship, owns, StringComparison.Ordinal)
            && WorkspaceResourceKinds.ByName(resource.ResourceType).ReleasedOnArchive)];

        foreach (var resource in released)
        {
            var retired = await Release(new WorkspaceResourceOwner(resource.Id, resource.ResourceId, workspace.Id), cancellationToken).ConfigureAwait(false);
            if (retired.IsFailure)
                return retired.ToNewResult<IReadOnlyList<WorkspaceResourceConfiguration>>();
        }

        WorkspaceResourceOwnersLog.ReleasedOnArchive(_logger, workspace.Name, released.Count);
        return GenericResult<IReadOnlyList<WorkspaceResourceConfiguration>>.Success(released);
    }
}
