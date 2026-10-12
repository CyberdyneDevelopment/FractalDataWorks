using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces.Abstractions;
using Fdw.Services.Workspaces.Results;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Changes the role a member holds on a workspace.</summary>
/// <remarks>
/// Version-on-write through the workspace aggregate's save, same as
/// <see cref="UpdateWorkspaceRelationshipEndpointBase"/>, carrying every other column forward from
/// the loaded row so only MemberRole and ModifyDate move. FDW-798 means the row's true CreateDate is
/// currently lost on this save regardless of what this endpoint sets -- filed, not fixed here.
/// </remarks>
public abstract class UpdateWorkspaceMemberRoleEndpointBase
    : CrudUpdateEndpointBase<UpdateWorkspaceMemberRoleRequest, WorkspaceMemberDto>
{
    private readonly WorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _access;

    // Set by FindForUpdate, read by Update -- both run within the same request via
    // CrudUpdateEndpointBase.HandleAsync, in that order, on a per-request endpoint instance.
    private WorkspaceMemberConfiguration? _existing;
    private IWorkspaceImplementationConfiguration? _workspace;

    /// <inheritdoc />
    protected UpdateWorkspaceMemberRoleEndpointBase(
        ILogger<UpdateWorkspaceMemberRoleEndpointBase> logger,
        WorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access) : base(logger)
    {
        _workspaces = workspaces;
        _access = access;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "workspaces";

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="IWorkspaceAccessPolicy.MayManageRole"/> decides, for the role the member holds and the
    /// role it would hold: a caller changes only memberships at or below their own rank, to a role at or
    /// below it (FDW-1071). The owner's own membership is never demoted.
    /// </remarks>
    protected override bool ResourceDecidesAccess => true;

    /// <inheritdoc />
    protected override string Route => "/workspaces/{Name}/members/{MemberId}";

    /// <inheritdoc />
    protected override string EndpointSummary => "Change a member's role";

    /// <inheritdoc />
    protected override string EndpointDescription => "Changes the role a member holds on the workspace.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(UpdateWorkspaceMemberRoleRequest request)
        => request.MemberId.ToString();

    /// <inheritdoc />
    protected override async Task<IGenericResult<WorkspaceMemberDto?>> FindForUpdate(
        UpdateWorkspaceMemberRoleRequest request, CancellationToken ct)
    {
        var workspace = await _workspaces.Get(request.Name, ct).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace.ToNewResult<WorkspaceMemberDto?>();
        if (workspace.Value is null) return GenericResult<WorkspaceMemberDto?>.Success(null);

        var permitted = await _access.MayManageMembers(workspace.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted.ToNewResult<WorkspaceMemberDto?>();

        // Found in the workspace's composed members. The aggregate is kept for the write: the change
        // is saved with the workspace, which writes its children under the workspace's current row.
        _workspace = workspace.Value;
        _existing = workspace.Value.Members.FirstOrDefault(x => x.Id == request.MemberId);
        return GenericResult<WorkspaceMemberDto?>.Success(_existing is null ? null : ToDto(_existing));
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<WorkspaceMemberDto>> Update(
        UpdateWorkspaceMemberRoleRequest request, WorkspaceMemberDto existing, CancellationToken ct)
    {
        // FindForUpdate always runs before Update in CrudUpdateEndpointBase.HandleAsync, and a null
        // _existing there already short-circuits to 404 before Update is ever called.
        var row = _existing!;

        var newRole = WorkspaceMemberRoles.ByName(request.MemberRole);
        if (ReferenceEquals(newRole, WorkspaceMemberRoles.NotFound))
        {
            return GenericResult<WorkspaceMemberDto>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceLifecycleValueInvalid"), Logger,
                ResultDetails.Create("name", request.Name, "field", "memberRole", "value", request.MemberRole));
        }

        var permitted = await MayMove(row, newRole, ct).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted.ToNewResult<WorkspaceMemberDto>();

        row.MemberRole = request.MemberRole;
        // Why stamped here: MsSqlConfigurationSaveTranslator's INSERT half writes every mapped
        // column explicitly, same as the create/attach sites (FDW-793) -- CreateDate is left as the
        // value already on `row`, though FDW-798 means that value is dropped regardless of what it
        // is (the translator excludes it from the INSERT unconditionally), not just here.
        row.ModifyDate = DateTimeOffset.UtcNow;

        // row is the instance inside the aggregate's Members, so the aggregate already carries the change.
        var saved = await _workspaces.Save(_workspace!, _workspace!.Domain, _workspace.Implementation, _workspace.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure) return saved.ToNewResult<WorkspaceMemberDto>();

        return GenericResult<WorkspaceMemberDto>.Success(ToDto(row));
    }

    // Both ends of the move are inside the caller's rank, and the owner's own membership stays Owner.
    private async Task<IGenericResult> MayMove(WorkspaceMemberConfiguration row, IWorkspaceMemberRole newRole, CancellationToken ct)
    {
        var from = await _access.MayManageRole(_workspace!, WorkspaceMemberRoles.ByName(row.MemberRole), ct).ConfigureAwait(false);
        if (from.IsFailure) return from;

        var to = await _access.MayManageRole(_workspace!, newRole, ct).ConfigureAwait(false);
        return to.IsFailure ? to : WorkspaceOwnerMembership.MayChange(_workspace!, row, newRole, Logger);
    }

    private static WorkspaceMemberDto ToDto(WorkspaceMemberConfiguration member) => new()
    {
        Id = member.Id,
        SubjectType = member.SubjectType,
        SubjectId = member.SubjectId,
        MemberRole = member.MemberRole,
        State = member.State,
        JoinedAt = member.JoinedAt,
    };
}
