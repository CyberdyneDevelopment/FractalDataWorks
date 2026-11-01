using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Workspaces.Logging;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Creates and removes a workspace's own permissions through the permission and role-permission providers.
/// </summary>
/// <remarks>
/// <para>
/// Written as ordinary permission rows, domain and implementation <c>Permission</c> like every seeded
/// one, with <c>PermissionDomain</c> <c>workspaces</c>, <c>Resource</c> <c>workspaces/{id}</c> and
/// <c>Action</c> <c>view</c> or <c>query</c>, so the name is <c>{Resource}:{Action}</c> as every seeded
/// permission's is. Scope is <c>tenant</c>: a workspace belongs to one tenant, and so do its permissions.
/// </para>
/// <para>
/// Removal takes the grants first and the permission second. The other order leaves grants pointing
/// at nothing; this order can at worst leave a permission nobody holds, which grants nothing.
/// </para>
/// </remarks>
public sealed class WorkspacePermissions : IWorkspacePermissions
{
    private const string PermissionDomainName = "Permission";

    private readonly IPermissionConfigurationProvider _permissions;
    private readonly IRolePermissionConfigurationProvider _grants;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="WorkspacePermissions"/> class.</summary>
    /// <param name="permissions">Reads and writes permission rows.</param>
    /// <param name="grants">Reads and removes role grants of them.</param>
    /// <param name="logger">The logger.</param>
    public WorkspacePermissions(
        IPermissionConfigurationProvider permissions,
        IRolePermissionConfigurationProvider grants,
        ILogger<WorkspacePermissions>? logger = null)
    {
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _grants = grants ?? throw new ArgumentNullException(nameof(grants));
        _logger = logger ?? NullLogger<WorkspacePermissions>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Create(Guid workspaceId, string workspaceName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceName);
        WorkspacePermissionsLog.CreatingPermissions(_logger, workspaceName, workspaceId.ToString("D", CultureInfo.InvariantCulture));

        var view = await Ensure(workspaceId, workspaceName, WorkspacePermissionNames.ViewAction, "View", cancellationToken).ConfigureAwait(false);
        return view.IsFailure
            ? view
            : await Ensure(workspaceId, workspaceName, WorkspacePermissionNames.QueryAction, "Query the data sets of", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Remove(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        WorkspacePermissionsLog.RemovingPermissions(_logger, workspaceId.ToString("D", CultureInfo.InvariantCulture));

        var view = await Retire(WorkspacePermissionNames.For(workspaceId, WorkspacePermissionNames.ViewAction), cancellationToken).ConfigureAwait(false);
        return view.IsFailure
            ? view
            : await Retire(WorkspacePermissionNames.For(workspaceId, WorkspacePermissionNames.QueryAction), cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult> Ensure(Guid workspaceId, string workspaceName, string action, string verb, CancellationToken cancellationToken)
    {
        var name = WorkspacePermissionNames.For(workspaceId, action);
        var existing = await _permissions.Get(name, cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure) return Failed("read", name, existing);
        if (existing.Value is not null)
        {
            WorkspacePermissionsLog.PermissionAlreadyExists(_logger, name);
            return GenericResult.Success();
        }

        var permission = new PermissionImplementationConfiguration
        {
            Name = name,
            Domain = PermissionDomainName,
            Implementation = PermissionDomainName,
            PermissionDomain = "workspaces",
            Resource = WorkspacePermissionNames.Resource(workspaceId),
            Action = action,
            Scope = "tenant",
            DisplayName = $"{verb} workspace {workspaceName}",
            Description = $"{verb} the workspace '{workspaceName}' only. Created with the workspace and removed with it.",
            Category = "Workspaces",
        };

        var saved = await _permissions.Save(permission, PermissionDomainName, PermissionDomainName, name, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return Failed("create", name, saved);

        WorkspacePermissionsLog.PermissionCreated(_logger, name, permission.DisplayName);
        return saved;
    }

    private async Task<IGenericResult> Retire(string permissionName, CancellationToken cancellationToken)
    {
        var existing = await _permissions.Get(permissionName, cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure) return Failed("read", permissionName, existing);
        if (existing.Value is not { } permission)
        {
            WorkspacePermissionsLog.PermissionAbsent(_logger, permissionName);
            return GenericResult.Success();
        }

        var grants = await _grants
            .Find<RolePermissionImplementationConfiguration>(grant => grant.PermissionId == permission.Id, cancellationToken)
            .ConfigureAwait(false);
        if (grants.IsFailure) return Failed("read the grants of", permissionName, grants);
        if (grants.Value is not { } held)
        {
            return GenericResult.Failure(
                WorkspacesResultCodes.ByName("WorkspaceLoadReturnedNoValue"), _logger,
                ResultDetails.Create("name", permissionName));
        }

        foreach (var grant in held)
        {
            var removed = await _grants.Delete(grant.Id, cancellationToken).ConfigureAwait(false);
            if (removed.IsFailure) return Failed("remove a grant of", permissionName, removed);
            WorkspacePermissionsLog.GrantRemoved(_logger, permissionName, grant.RoleId.ToString("D", CultureInfo.InvariantCulture));
        }

        var deleted = await _permissions.Delete(permission.Id, cancellationToken).ConfigureAwait(false);
        if (deleted.IsFailure) return Failed("remove", permissionName, deleted);

        WorkspacePermissionsLog.PermissionRemoved(_logger, permissionName, held.Count);
        return deleted;
    }

    // The provider has already logged its own failure; this records which step of which permission it
    // was, and hands the failure back unchanged.
    private IGenericResult Failed(string step, string permissionName, IGenericResult failure)
    {
        WorkspacePermissionsLog.PermissionStepFailed(
            _logger, step, permissionName, string.Join(" <- ", failure.CodeChain.Select(code => code.Name)));
        return failure;
    }
}
