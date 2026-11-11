using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Makes an organization's default workspace, the workspace where a thing the organization owns and that names no workspace is placed
/// (FDW-3668). The workspace is written through the workspace provider, so it is versioned and cached like any other, and its permissions
/// are made with it by the step the create endpoint uses.
/// </summary>
public sealed class DefaultWorkspaceFounding : IDefaultWorkspaceFounding
{
    private const string DomainName = "Workspace";

    // The lifecycle values are stated, not defaulted: a project the platform makes for an organization is visible to nobody else
    // until someone opens it, and a closed workspace auto-approves nobody (FDW-1060, FDW-1070).
    private const string StatusActive = "Active";
    private const string VisibilityPrivate = "Private";
    private const string JoinPolicyClosed = "Closed";

    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspacePermissions _permissions;
    private readonly IAuthenticationContextAccessor _authentication;
    private readonly ILogger<DefaultWorkspaceFounding> _logger;

    /// <summary>Initializes a new instance of the <see cref="DefaultWorkspaceFounding"/> class.</summary>
    /// <param name="workspaces">The workspace provider.</param>
    /// <param name="permissions">The workspace permissions service.</param>
    /// <param name="authentication">The accessor of the caller's authentication context.</param>
    /// <param name="logger">The logger.</param>
    public DefaultWorkspaceFounding(
        IWorkspaceConfigurationProvider workspaces,
        IWorkspacePermissions permissions,
        IAuthenticationContextAccessor authentication,
        ILogger<DefaultWorkspaceFounding>? logger = null)
    {
        _workspaces = workspaces;
        _permissions = permissions;
        _authentication = authentication;
        _logger = logger ?? NullLogger<DefaultWorkspaceFounding>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> Found(NewDefaultWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var configuration = ToConfiguration(workspace);
        DefaultWorkspaceLog.Founding(_logger, configuration.Name, workspace.OrganizationId, workspace.TenantId);

        using var acting = TenantActing.In(_authentication, workspace.TenantId, workspace.OwnerUserId, _logger);
        var saved = await _workspaces.Save(configuration, DomainName, configuration.Implementation, configuration.Name, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure) return saved.ToNewResult<Guid>();

        var permitted = await WorkspacePermissionsOrUndo
            .Create(_permissions, _workspaces, configuration.Id, configuration.Name, _logger, cancellationToken).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted.ToNewResult<Guid>();

        DefaultWorkspaceLog.Founded(_logger, configuration.Name, configuration.Id);
        return GenericResult<Guid>.Success(configuration.Id);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Remove(NewDefaultWorkspace workspace, CancellationToken cancellationToken = default)
    {
        DefaultWorkspaceLog.Removing(_logger, workspace.WorkspaceId, workspace.TenantId);
        using var acting = TenantActing.In(_authentication, workspace.TenantId, workspace.OwnerUserId, _logger);
        var retired = await _permissions.Remove(workspace.WorkspaceId, cancellationToken).ConfigureAwait(false);
        var deleted = await _workspaces.Delete(workspace.WorkspaceId, cancellationToken).ConfigureAwait(false);
        return retired.IsFailure ? retired : deleted;
    }

    // The workspace is named after the thing that owns it, exactly: no slug, no 'Default' and no 'Workspace' (FDW-4186, Mike: "workspace adds
    // nothing since we will be dealing with a workspace, and default doesn't help any since they won't have another primary workspace").
    // A name another workspace of the tenant already has is refused by the unique index on (Name, TenantId), not renamed here.
    private static WorkspaceImplementationConfiguration ToConfiguration(NewDefaultWorkspace workspace) => new()
    {
        Id = workspace.WorkspaceId,
        Implementation = DomainName,
        CreateDate = DateTimeOffset.UtcNow,
        OwnerUserId = workspace.OwnerUserId,
        Name = workspace.OwnerName,
        DisplayName = workspace.OwnerName,
        Description = string.Concat("The data sets, pipelines and data stores that ", workspace.OwnerName, " owns."),
        Purpose = string.Concat("Where the things that ", workspace.OwnerName, " owns are found."),
        Status = StatusActive,
        Visibility = VisibilityPrivate,
        JoinPolicy = JoinPolicyClosed,
        OrganizationId = workspace.OrganizationId,
    };
}
